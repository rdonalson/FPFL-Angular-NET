import { apiUrl } from '../../../core/api/api-response';

/**
 * Probes API-NET's /health and /info (the same Actuator-style shapes API-JDK serves, so this
 * mirrors UI-TSX's healthApi). Plain fetch rather than HttpClient: no auth header, no global
 * loading bar, no error toasts, and the result says *why* a probe failed.
 */

export const HEALTH_URL = apiUrl('/health');
export const INFO_URL = apiUrl('/info');

/** A status page should report "not answering" quickly rather than wait for the API's timeout. */
export const PROBE_TIMEOUT_MS = 5000;

export type HealthStatus = 'UP' | 'DOWN' | 'DEGRADED' | 'OUT_OF_SERVICE' | 'UNKNOWN' | 'BLOCKED' | (string & {});

export interface HealthComponent {
  status: HealthStatus;
  description?: string | null;
  durationMs?: number;
  details?: Record<string, unknown>;
}

export interface ApiHealth {
  status: HealthStatus;
  components?: Record<string, HealthComponent>;
}

export interface ApiInfo {
  app?: { name?: string; description?: string; version?: string };
  build?: { name?: string; artifact?: string; version?: string; commit?: string | null; time?: string | null };
  runtime?: { framework?: string; os?: string; environment?: string; startedAt?: string; uptimeSeconds?: number };
}

export interface ProbeResult<T> {
  ok: boolean;
  /** HTTP status code, or 0 when the request never reached the server. */
  httpStatus: number;
  latencyMs: number;
  url: string;
  body?: T;
  error?: string;
  /** A short "what to check" line for a failure. */
  hint?: string;
  /** Something answered, but the browser would not let this page read it (CORS). */
  blocked?: boolean;
  checkedAt: Date;
}

export function probeHealth(): Promise<ProbeResult<ApiHealth>> {
  return probe<ApiHealth>(HEALTH_URL);
}

export function probeInfo(): Promise<ProbeResult<ApiInfo>> {
  return probe<ApiInfo>(INFO_URL);
}

/** GETs JSON, timing the call. Never throws: failures come back as ok: false with a reason. */
export async function probe<T>(url: string, timeoutMs = PROBE_TIMEOUT_MS): Promise<ProbeResult<T>> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  const started = performance.now();

  try {
    const response = await fetch(url, {
      headers: { Accept: 'application/json' },
      cache: 'no-store',
      signal: controller.signal,
    });
    const latencyMs = Math.round(performance.now() - started);

    // /health answers 503 with a JSON body when a component is DOWN, so parse either way.
    const text = await response.text();
    let body: T | undefined;
    try {
      body = JSON.parse(text) as T;
    } catch {
      body = undefined;
    }

    let hint: string | undefined;
    if (!body) {
      hint = `The response wasn't JSON, so ${url} is probably not the API.`;
    } else if (response.status === 401 || response.status === 403) {
      hint = 'The endpoint needs authentication; /health and /info should allow anonymous access.';
    } else if (response.status === 404) {
      hint = 'Endpoint not found. Check that this API version maps /health and /info.';
    }

    return {
      ok: response.ok && body !== undefined,
      httpStatus: response.status,
      latencyMs,
      url,
      body,
      error: response.ok ? undefined : `HTTP ${response.status} ${response.statusText}`.trim(),
      hint,
      checkedAt: new Date(),
    };
  } catch (error) {
    const latencyMs = Math.round(performance.now() - started);
    const aborted = error instanceof DOMException && error.name === 'AbortError';
    const blocked = !aborted && (await isReachable(url, timeoutMs));

    return {
      ok: false,
      httpStatus: 0,
      latencyMs,
      url,
      blocked,
      error: aborted
        ? `No response within ${timeoutMs / 1000}s`
        : blocked
          ? 'Blocked by the browser (CORS)'
          : error instanceof Error
            ? error.message
            : 'Request failed',
      hint: aborted
        ? 'The API accepted the connection but never answered.'
        : blocked
          ? `The API answered at ${url}, but the browser refused to hand the response to ${location.origin}. ` +
            "Add this origin to the API's Cors:AllowedOrigins."
          : `Nothing answered at ${url}. Check that the API is running and environment.apiUrl points at it.`,
      checkedAt: new Date(),
    };
  } finally {
    clearTimeout(timer);
  }
}

/**
 * A no-cors request resolves only if something answered, which separates "the API is down"
 * from "the API is up and CORS blocked the read" (the browser reports both as a TypeError).
 */
async function isReachable(url: string, timeoutMs: number): Promise<boolean> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    await fetch(url, { mode: 'no-cors', cache: 'no-store', signal: controller.signal });
    return true;
  } catch {
    return false;
  } finally {
    clearTimeout(timer);
  }
}

/** PrimeNG tag severity for a status. */
export function statusSeverity(status: HealthStatus): 'success' | 'danger' | 'warn' {
  switch (status) {
    case 'UP':
      return 'success';
    case 'DOWN':
    case 'OUT_OF_SERVICE':
      return 'danger';
    default:
      return 'warn';
  }
}

/** 3725000 → "1h 2m"; 65000 → "1m 5s"; 4000 → "4s". */
export function formatDuration(ms: number): string {
  const total = Math.max(0, Math.floor(ms / 1000));
  const hours = Math.floor(total / 3600);
  const minutes = Math.floor((total % 3600) / 60);
  const seconds = total % 60;
  if (hours) {
    return `${hours}h ${minutes}m`;
  }
  if (minutes) {
    return `${minutes}m ${seconds}s`;
  }
  return `${seconds}s`;
}

/** Bytes in the largest sensible unit: 1536 → "1.5 KB". */
export function formatBytes(bytes: number): string {
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  let value = bytes;
  let unit = 0;
  while (Math.abs(value) >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit++;
  }
  return `${unit === 0 ? value : value.toFixed(1)} ${units[unit]}`;
}

/** A component's details as one line: "database: PostgreSQL · name: FPFL-V2". Sizes are humanized. */
export function formatDetails(component: HealthComponent): string {
  const parts: string[] = [];
  if (component.description) {
    parts.push(component.description);
  }
  for (const [key, value] of Object.entries(component.details ?? {})) {
    const shown =
      typeof value === 'number' && ['total', 'free', 'threshold'].includes(key)
        ? formatBytes(value)
        : typeof value === 'object'
          ? JSON.stringify(value)
          : String(value);
    parts.push(`${key}: ${shown}`);
  }
  if (component.durationMs !== undefined) {
    parts.push(`${component.durationMs} ms`);
  }
  return parts.join(' · ') || '—';
}

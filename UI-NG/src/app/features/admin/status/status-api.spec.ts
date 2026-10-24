import {
  HEALTH_URL,
  INFO_URL,
  formatBytes,
  formatDetails,
  formatDuration,
  probe,
  probeHealth,
  probeInfo,
  statusSeverity,
} from './status-api';

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

describe('status-api', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('probes /health and /info on the API', async () => {
    // A fresh Response per call: a body can only be read once.
    const fetchMock = vi.fn((_url: string) => Promise.resolve(json({ status: 'UP' })));
    vi.stubGlobal('fetch', fetchMock);

    await probeHealth();
    await probeInfo();

    expect(HEALTH_URL).toMatch(/\/health$/);
    expect(INFO_URL).toMatch(/\/info$/);
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([HEALTH_URL, INFO_URL]);
  });

  it('returns the body, status and latency of a healthy API', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ status: 'UP', components: { db: { status: 'UP' } } })));

    const result = await probe<{ status: string }>(HEALTH_URL);

    expect(result.ok).toBe(true);
    expect(result.httpStatus).toBe(200);
    expect(result.body?.status).toBe('UP');
    expect(result.latencyMs).toBeGreaterThanOrEqual(0);
    expect(result.error).toBeUndefined();
  });

  it('still reads the body of a 503 (a component is DOWN)', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ status: 'DOWN' }, 503)));

    const result = await probe<{ status: string }>(HEALTH_URL);

    expect(result.ok).toBe(false);
    expect(result.httpStatus).toBe(503);
    expect(result.body?.status).toBe('DOWN');
    expect(result.error).toContain('HTTP 503');
  });

  it('explains a response that is not JSON, or an endpoint that is missing', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<html>', { status: 200 })));
    const notJson = await probe(HEALTH_URL);
    expect(notJson.ok).toBe(false);
    expect(notJson.hint).toContain("wasn't JSON");

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ status: 404 }, 404)));
    expect((await probe(HEALTH_URL)).hint).toContain('Endpoint not found');
  });

  it('tells CORS blocking apart from an API that is down', async () => {
    // The normal request fails, but a no-cors request gets through: something answered.
    vi.stubGlobal(
      'fetch',
      vi.fn((_url: string, init?: RequestInit) =>
        init?.mode === 'no-cors' ? Promise.resolve(new Response(null)) : Promise.reject(new TypeError('Failed to fetch')),
      ),
    );
    const blocked = await probe(HEALTH_URL);
    expect(blocked).toMatchObject({ ok: false, httpStatus: 0, blocked: true, error: 'Blocked by the browser (CORS)' });
    expect(blocked.hint).toContain('Cors:AllowedOrigins');

    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
    const down = await probe(HEALTH_URL);
    expect(down).toMatchObject({ ok: false, blocked: false, error: 'Failed to fetch' });
    expect(down.hint).toContain('Nothing answered');
  });

  it('gives up after the timeout', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(
        (_url: string, init?: RequestInit) =>
          new Promise((_resolve, reject) =>
            init?.signal?.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError'))),
          ),
      ),
    );

    const result = await probe(HEALTH_URL, 20);

    expect(result.ok).toBe(false);
    expect(result.error).toBe('No response within 0.02s');
    expect(result.hint).toContain('never answered');
  });

  it('formats durations, sizes and component details', () => {
    expect(formatDuration(4_000)).toBe('4s');
    expect(formatDuration(65_000)).toBe('1m 5s');
    expect(formatDuration(3_725_000)).toBe('1h 2m');
    expect(formatBytes(512)).toBe('512 B');
    expect(formatBytes(1536)).toBe('1.5 KB');
    expect(formatBytes(10 * 1024 * 1024)).toBe('10.0 MB');

    expect(
      formatDetails({
        status: 'UP',
        description: 'Free space above threshold',
        durationMs: 0.4,
        details: { free: 2 * 1024 ** 3, threshold: 10 * 1024 ** 2, exists: true },
      }),
    ).toBe('Free space above threshold · free: 2.0 GB · threshold: 10.0 MB · exists: true · 0.4 ms');
    expect(formatDetails({ status: 'UP' })).toBe('—');
  });

  it('maps statuses to tag colors', () => {
    expect(statusSeverity('UP')).toBe('success');
    expect(statusSeverity('DOWN')).toBe('danger');
    expect(statusSeverity('DEGRADED')).toBe('warn');
    expect(statusSeverity('UNKNOWN')).toBe('warn');
  });
});

import { JsonPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, VERSION, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';

import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth';
import {
  ApiHealth,
  ApiInfo,
  HEALTH_URL,
  HealthStatus,
  INFO_URL,
  ProbeResult,
  formatDetails,
  formatDuration,
  probeHealth,
  probeInfo,
  statusSeverity,
} from './status-api';

/** How often the page re-checks while Auto is on (the same 30 seconds as UI-TSX). */
export const POLL_INTERVAL_MS = 30_000;

/** When this browser tab loaded the app, for "Page uptime". */
const APP_STARTED_AT = Date.now();

export interface CheckRow {
  name: string;
  status: HealthStatus;
  detail: string;
}

/**
 * Admin-only System Status (UI-TSX's StatusPage): the UI's own checks, API-NET's /health
 * components (db, diskSpace, ping) with latency, version and build time from /info, a
 * 30-second auto refresh, and the raw responses.
 */
@Component({
  selector: 'app-status',
  imports: [RouterLink, JsonPipe, ButtonModule, MessageModule, TagModule],
  templateUrl: './status.html',
  styleUrls: ['../../item-types/item-types.scss', './status.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Status {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly healthUrl = HEALTH_URL;
  protected readonly infoUrl = INFO_URL;
  protected readonly pollSeconds = POLL_INTERVAL_MS / 1000;

  protected readonly health = signal<ProbeResult<ApiHealth> | null>(null);
  protected readonly info = signal<ProbeResult<ApiInfo> | null>(null);
  protected readonly loading = signal(false);
  protected readonly autoRefresh = signal(true);
  protected readonly online = signal(typeof navigator === 'undefined' ? true : navigator.onLine);
  private readonly now = signal(Date.now());

  private timer: ReturnType<typeof setInterval> | undefined;

  /** UP / DOWN / … for the API card's tag, or why there is no answer. */
  protected readonly apiStatus = computed<HealthStatus>(() => {
    const health = this.health();
    if (!health) {
      return 'UNKNOWN';
    }
    return health.body?.status ?? (health.ok ? 'UP' : health.blocked ? 'BLOCKED' : 'DOWN');
  });

  protected readonly uiChecks = computed<CheckRow[]>(() => {
    const session = this.auth.session();
    const health = this.health();
    return [
      { name: 'UI rendering', status: 'UP', detail: `Angular ${VERSION.full}` },
      {
        name: 'Browser connection',
        status: this.online() ? 'UP' : 'DOWN',
        detail: this.online() ? 'Online' : 'Offline (no network)',
      },
      {
        name: 'Session',
        status: session ? 'UP' : 'UNKNOWN',
        detail: session
          ? `${session.email} · ${session.roles.join(', ') || 'no roles'}`
          : 'Not signed in',
      },
      { name: 'Router', status: 'UP', detail: this.router.url },
      { name: 'API base URL', status: environment.apiUrl ? 'UP' : 'UNKNOWN', detail: environment.apiUrl || '—' },
      {
        name: 'Health endpoint',
        status: health ? (health.ok ? 'UP' : 'DOWN') : 'UNKNOWN',
        detail: HEALTH_URL,
      },
      { name: 'UI build', status: 'UP', detail: environment.production ? 'production' : 'development' },
      { name: 'Page uptime', status: 'UP', detail: formatDuration(this.now() - APP_STARTED_AT) },
    ];
  });

  protected readonly apiComponents = computed<CheckRow[]>(() =>
    Object.entries(this.health()?.body?.components ?? {}).map(([name, component]) => ({
      name,
      status: component.status,
      detail: formatDetails(component),
    })),
  );

  protected readonly build = computed(() => this.info()?.body?.build ?? null);
  protected readonly runtime = computed(() => this.info()?.body?.runtime ?? null);
  protected readonly buildTime = computed(() => {
    const time = this.build()?.time;
    return time ? new Date(time).toLocaleString() : null;
  });
  protected readonly apiUptime = computed(() => {
    const seconds = this.runtime()?.uptimeSeconds;
    return seconds === undefined ? null : formatDuration(seconds * 1000);
  });
  protected readonly checkedAt = computed(() => this.health()?.checkedAt.toLocaleTimeString() ?? null);

  /** What the "Raw responses" section shows. */
  protected readonly raw = computed(() => ({
    health: this.health()?.body ?? this.health()?.error ?? null,
    info: this.info()?.body ?? this.info()?.error ?? null,
  }));

  constructor() {
    const updateOnline = () => this.online.set(navigator.onLine);
    window.addEventListener('online', updateOnline);
    window.addEventListener('offline', updateOnline);

    inject(DestroyRef).onDestroy(() => {
      this.stopTimer();
      window.removeEventListener('online', updateOnline);
      window.removeEventListener('offline', updateOnline);
    });

    void this.refresh();
    this.startTimer();
  }

  protected readonly severity = statusSeverity;

  /** Probes /health and /info together. */
  async refresh(): Promise<void> {
    this.loading.set(true);
    const [health, info] = await Promise.all([probeHealth(), probeInfo()]);
    this.health.set(health);
    this.info.set(info);
    this.now.set(Date.now());
    this.loading.set(false);
  }

  protected toggleAutoRefresh(): void {
    this.autoRefresh.update((on) => !on);
    if (this.autoRefresh()) {
      this.startTimer();
    } else {
      this.stopTimer();
    }
  }

  private startTimer(): void {
    this.stopTimer();
    this.timer = setInterval(() => void this.refresh(), POLL_INTERVAL_MS);
  }

  private stopTimer(): void {
    if (this.timer !== undefined) {
      clearInterval(this.timer);
      this.timer = undefined;
    }
  }
}

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { appTestProviders } from '../../../../testing/test-providers';
import { Status } from './status';
import { HEALTH_URL, INFO_URL } from './status-api';

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

const HEALTH = {
  status: 'UP',
  components: {
    db: { status: 'UP', description: 'Database reachable', durationMs: 3.2, details: { database: 'PostgreSQL' } },
    diskSpace: { status: 'UP', details: { free: 5 * 1024 ** 3, threshold: 10 * 1024 ** 2 } },
    ping: { status: 'UP' },
  },
};

const INFO = {
  app: { name: 'FPFL API', version: '1.0.0' },
  build: { name: 'Fpfl.Api', artifact: 'api-net', version: '1.0.0', commit: '3f2a9c1', time: '2026-10-24T16:00:00Z' },
  runtime: { framework: '.NET 10.0.0', os: 'Linux', environment: 'Production', startedAt: '2026-10-24T16:00:00Z', uptimeSeconds: 3900 },
};

describe('Status', () => {
  let fixture: ComponentFixture<Status>;
  /** URLs fetched (not counting the no-cors reachability checks). */
  let fetched: string[];

  async function render(health: Response | Error, info: Response | Error = json(INFO)): Promise<void> {
    fetched = [];
    const fetchMock = vi.fn((url: string, init?: RequestInit) => {
      if (init?.mode === 'no-cors') {
        return Promise.reject(new TypeError('Failed to fetch'));
      }
      fetched.push(url);
      const result = url === HEALTH_URL ? health : info;
      return result instanceof Error ? Promise.reject(result) : Promise.resolve(result.clone());
    });
    vi.stubGlobal('fetch', fetchMock);

    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [Status],
      providers: appTestProviders(),
    }).compileComponents();

    fixture = TestBed.createComponent(Status);
    await settle();
  }

  /** Lets the probes resolve, then renders. */
  async function settle(): Promise<void> {
    for (let i = 0; i < 5; i++) {
      await new Promise((resolve) => setTimeout(resolve, 0));
    }
    fixture.detectChanges();
    await fixture.whenStable();
  }

  afterEach(() => {
    fixture?.destroy();
    vi.unstubAllGlobals();
  });

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists the UI checks', async () => {
    await render(json(HEALTH));

    for (const check of ['UI rendering', 'Browser connection', 'Session', 'API base URL', 'Health endpoint', 'Page uptime']) {
      expect(text()).toContain(check);
    }
    expect(text()).toContain('Not signed in');
  });

  it("shows the API's components, version, build and runtime from /health and /info", async () => {
    await render(json(HEALTH));

    expect(fetched).toEqual([HEALTH_URL, INFO_URL]);
    for (const component of ['db', 'diskSpace', 'ping']) {
      expect(text()).toContain(component);
    }
    expect(text()).toContain('Database reachable');
    expect(text()).toContain('free: 5.0 GB');
    expect(text()).toContain('v1.0.0');
    expect(text()).toContain('(3f2a9c1)');
    expect(text()).toContain('.NET 10.0.0');
    expect(text()).toContain('Production');
    expect(text()).toContain('Up 1h 5m');
    expect(text()).toContain('HTTP 200');
    expect(text()).toContain('Checked');
  });

  it('explains a failed check', async () => {
    await render(new TypeError('Failed to fetch'), new TypeError('Failed to fetch'));

    expect(text()).toContain('API check failed: Failed to fetch');
    expect(text()).toContain('Nothing answered');
    expect(text()).toContain('No component detail while the API is unreachable.');
    expect(text()).toContain('no response');
  });

  it('shows a DOWN component from a 503', async () => {
    await render(json({ status: 'DOWN', components: { db: { status: 'DOWN', description: 'Cannot connect to database' } } }, 503));

    expect(text()).toContain('Cannot connect to database');
    expect(text()).toContain('HTTP 503');
  });

  it('refreshes on demand and can pause the 30-second auto refresh', async () => {
    await render(json(HEALTH));
    expect(fetched).toHaveLength(2);

    await fixture.componentInstance.refresh();
    expect(fetched).toEqual([HEALTH_URL, INFO_URL, HEALTH_URL, INFO_URL]);

    const el = fixture.nativeElement as HTMLElement;
    const pause = el.querySelector<HTMLButtonElement>('button[aria-label="Pause auto refresh"]');
    expect(pause).toBeTruthy();
    pause!.click();
    await settle();
    expect(el.querySelector('button[aria-label="Resume auto refresh"]')).toBeTruthy();
    expect(text()).toContain('Paused');
  });

  it('includes the raw responses', async () => {
    await render(json(HEALTH));
    const raw = (fixture.nativeElement as HTMLElement).querySelector('.status__raw pre')?.textContent ?? '';
    expect(raw).toContain('"diskSpace"');
    expect(raw).toContain('"artifact": "api-net"');
  });
});

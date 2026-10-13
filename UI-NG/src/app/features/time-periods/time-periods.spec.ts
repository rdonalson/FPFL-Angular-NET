import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, envelope } from '../../../testing/test-providers';
import { apiUrl } from '../../core/api/api-response';
import { TimePeriods } from './time-periods';

describe('TimePeriods', () => {
  let fixture: ComponentFixture<TimePeriods>;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [TimePeriods],
      providers: appTestProviders(),
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(TimePeriods);
    fixture.detectChanges();
    TestBed.tick(); // lets httpResource send its request
  });

  afterEach(() => http.verify());

  const el = () => fixture.nativeElement as HTMLElement;

  it('lists the time periods returned by the API', async () => {
    http.expectOne(apiUrl('/time-periods')).flush(
      envelope([
        { id: 1, name: 'One Time' },
        { id: 2, name: 'Daily' },
        { id: 3, name: 'Weekly' },
      ]),
    );
    await fixture.whenStable();

    expect(el().textContent).toContain('One Time');
    expect(el().textContent).toContain('Weekly');
  });

  it('hides the admin actions from non-admins', async () => {
    http.expectOne(apiUrl('/time-periods')).flush(envelope([{ id: 1, name: 'One Time' }]));
    await fixture.whenStable();

    expect(el().querySelector('[aria-label="Edit One Time"]')).toBeNull();
    expect(el().textContent).not.toContain('Add');
  });

  it('shows the API error message and reference', async () => {
    http
      .expectOne(apiUrl('/time-periods'))
      .flush(envelope(null, 500, 'An unexpected error occurred.', 'ref-7'), {
        status: 500,
        statusText: 'Server Error',
      });
    await fixture.whenStable();

    expect(el().textContent).toContain('An unexpected error occurred.');
    expect(el().textContent).toContain('ref-7');
  });
});

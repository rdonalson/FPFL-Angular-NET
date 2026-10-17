import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, authResponse, envelope } from '../../../../testing/test-providers';
import { apiUrl } from '../../../core/api/api-response';
import { toSession } from '../../../core/auth/auth.models';
import { InitialAmount } from '../item';
import { InitialAmountPage } from './initial-amount';

describe('InitialAmountPage', () => {
  let fixture: ComponentFixture<InitialAmountPage>;
  let http: HttpTestingController;
  const userId = authResponse().userId;
  const ia = (amount: number): InitialAmount => ({
    id: 11, userId, name: 'IA', amount, itemType: { id: 3, name: 'Initial Amount' }, beginDate: '2026-10-17',
  });

  beforeEach(async () => {
    sessionStorage.clear();
    sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse())));
    await TestBed.configureTestingModule({ imports: [InitialAmountPage], providers: appTestProviders() }).compileComponents();
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(InitialAmountPage);
    fixture.detectChanges();
    TestBed.tick();
  });

  afterEach(() => http.verify());

  const el = () => fixture.nativeElement as HTMLElement;
  const button = (label: string) =>
    Array.from(el().querySelectorAll<HTMLButtonElement>('button')).find((b) => b.textContent?.trim() === label)!;

  it("shows the user's starting balance and when it was set", async () => {
    http.expectOne(apiUrl(`/initial-amount/${userId}`)).flush(envelope(ia(2500)));
    await fixture.whenStable();

    expect(el().textContent).toContain('$2,500.00');
    expect(el().textContent).toContain('Set on Oct 17, 2026');
  });

  it('edits the amount with a PUT and reloads it', async () => {
    http.expectOne(apiUrl(`/initial-amount/${userId}`)).flush(envelope(ia(0)));
    await fixture.whenStable();

    button('Edit').click();
    await fixture.whenStable();
    (fixture.componentInstance as unknown as { draft: { set(v: number): void } }).draft.set(1800);
    el().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const req = http.expectOne(apiUrl('/initial-amount/11'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ amount: 1800 });
    req.flush(envelope(ia(1800), 200, 'InitialAmount updated successfully'));

    TestBed.tick();
    http.expectOne(apiUrl(`/initial-amount/${userId}`)).flush(envelope(ia(1800)));
    await fixture.whenStable();

    expect(el().textContent).toContain('$1,800.00');
  });

  it('rejects a negative amount without calling the API', async () => {
    http.expectOne(apiUrl(`/initial-amount/${userId}`)).flush(envelope(ia(100)));
    await fixture.whenStable();

    button('Edit').click();
    await fixture.whenStable();
    (fixture.componentInstance as unknown as { draft: { set(v: number): void } }).draft.set(-5);
    el().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    expect(el().textContent).toContain('Enter an amount of $0.00 or more.');
  });
});

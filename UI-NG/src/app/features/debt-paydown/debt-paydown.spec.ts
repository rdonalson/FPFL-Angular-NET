import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpRequest } from '@angular/common/http';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, authResponse, envelope } from '../../../testing/test-providers';
import { apiUrl } from '../../core/api/api-response';
import { toSession } from '../../core/auth/auth.models';
import { toIsoDate } from '../item-detail/item-form';
import { DebtPaydownPage } from './debt-paydown';
import { AllocationForm, Debt, DebtForm } from './debt-paydown.models';
import { bonus, carLoan, chaseDebt, chaseVisa, paycheck } from './testing-debts';

/** The page's protected methods, for driving the editor without PrimeNG's overlays. */
interface PageApi {
  openNew(): void;
  openEdit(debt: Debt): void;
  patch(changes: Partial<DebtForm>): void;
  addAllocation(): void;
  patchAllocation(key: number, changes: Partial<AllocationForm>): void;
  save(): void;
  confirmDelete(debt: Debt): void;
  delete(): void;
  form(): DebtForm;
  problem(): string | null;
}

describe('DebtPaydownPage', () => {
  let fixture: ComponentFixture<DebtPaydownPage>;
  let http: HttpTestingController;
  const userId = authResponse().userId;

  const debtsUrl = apiUrl('/debts');
  const is = (method: string, url: string) => (r: HttpRequest<unknown>) => r.method === method && r.url === url;

  async function create(debts: Debt[] = [chaseDebt()]) {
    sessionStorage.clear();
    sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse())));
    await TestBed.configureTestingModule({ imports: [DebtPaydownPage], providers: appTestProviders() }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(DebtPaydownPage);
    fixture.detectChanges();
    TestBed.tick();

    http.expectOne(is('GET', debtsUrl)).flush(envelope(debts));
    http.expectOne(apiUrl(`/items/${userId}/2`)).flush(envelope([chaseVisa, carLoan]));
    http.expectOne(apiUrl(`/items/${userId}/1`)).flush(envelope([paycheck, bonus]));
    await fixture.whenStable();
  }

  /** After a save or delete the list reloads. */
  async function flushReload(debts: Debt[]) {
    TestBed.tick();
    http.expectOne(is('GET', debtsUrl)).flush(envelope(debts));
    await fixture.whenStable();
  }

  afterEach(() => http.verify());

  const page = () => fixture.componentInstance as unknown as PageApi;
  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists tracked debts with their balance, APR and allocations', async () => {
    await create();

    expect(text()).toContain('Chase Visa');
    expect(text()).toContain('Payment $200.00');
    expect(text()).toContain('$4,500.00');
    expect(text()).toContain('24.99% APR');
    expect(text()).toContain('70%');
    expect(text()).toContain('Paycheck');
    expect(text()).toContain('$1,400.00 each');
  });

  it('explains how to start when nothing is tracked', async () => {
    await create([]);

    expect(text()).toContain('Pick a debit');
  });

  it('tracks a new debt with an allocation', async () => {
    await create();

    page().openNew();
    page().patch({ itemId: 11, balance: 1000 });
    page().addAllocation();
    page().patchAllocation(page().form().allocations[0].key, { creditItemId: 21, percent: 50 });
    page().save();

    const request = http.expectOne(is('POST', debtsUrl));
    expect(request.request.body).toEqual({
      itemId: 11,
      balance: 1000,
      apr: 0,
      balanceDate: toIsoDate(new Date()),
      allocations: [{ creditItemId: 21, percent: 50 }],
    });
    request.flush(envelope(chaseDebt({ id: 2, itemId: 11, name: 'Car Loan' }), 201, 'Debt created successfully'));
    await flushReload([chaseDebt()]);
  });

  it('stops a credit being allocated past 100% before calling the API', async () => {
    await create();

    page().openNew();
    page().patch({ itemId: 11, balance: 100 });
    page().addAllocation();
    page().patchAllocation(page().form().allocations[0].key, { creditItemId: 20, percent: 40 });
    page().save();

    expect(page().problem()).toBe('Paycheck is already 70% allocated to other debts; at most 30% is left.');
    http.expectNone(is('POST', debtsUrl));
  });

  it('edits a debt, letting it raise its own share', async () => {
    await create();

    page().openEdit(chaseDebt());
    page().patchAllocation(page().form().allocations[0].key, { percent: 90 });
    page().patch({ balance: 4200 });
    page().save();

    const request = http.expectOne(is('PUT', apiUrl('/debts/1')));
    expect(request.request.body).toMatchObject({ balance: 4200, apr: 24.99, allocations: [{ creditItemId: 20, percent: 90 }] });
    request.flush(envelope(chaseDebt({ balance: 4200 }), 200, 'Debt updated successfully'));
    await flushReload([chaseDebt({ balance: 4200 })]);
  });

  it('stops tracking a debt after confirming', async () => {
    await create();

    page().confirmDelete(chaseDebt());
    page().delete();

    http.expectOne(is('DELETE', apiUrl('/debts/1'))).flush(envelope(null, 200, 'Debt deleted successfully'));
    await flushReload([]);
  });
});

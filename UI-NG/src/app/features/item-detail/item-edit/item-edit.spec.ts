import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { FormGroup } from '@angular/forms';
import { Router } from '@angular/router';

import { appTestProviders, authResponse, envelope } from '../../../../testing/test-providers';
import { apiUrl } from '../../../core/api/api-response';
import { toSession } from '../../../core/auth/auth.models';
import { PERIOD } from '../item';
import { makeItem } from '../testing-items';
import { ItemEdit } from './item-edit';

describe('ItemEdit', () => {
  let fixture: ComponentFixture<ItemEdit>;
  let http: HttpTestingController;
  let navigated: string[];

  async function create(inputs: Record<string, unknown>) {
    sessionStorage.clear();
    sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse())));
    await TestBed.configureTestingModule({ imports: [ItemEdit], providers: appTestProviders() }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    navigated = [];
    vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockImplementation((url) => {
      navigated.push(String(url));
      return Promise.resolve(true);
    });

    fixture = TestBed.createComponent(ItemEdit);
    for (const [name, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(name, value);
    }
    fixture.detectChanges();
    TestBed.tick();
    // On the edit route the item request is still open, and whenStable() would wait for it
    // forever; those tests flush it first and then wait.
    if (!('id' in inputs)) {
      await fixture.whenStable();
    }
  }

  afterEach(() => http.verify());

  const el = () => fixture.nativeElement as HTMLElement;
  // The form is protected; tests reach it the way the template does.
  const form = () => (fixture.componentInstance as unknown as { form: FormGroup }).form;
  const submit = async () => {
    el().querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  };

  it('starts a new item with the period chosen on the picker', async () => {
    await create({ itemType: 1, periodId: String(PERIOD.monthly) });

    expect(el().textContent).toContain('New Credit');
    expect(el().textContent).toContain('Monthly credit');
    expect(el().textContent).toContain('Day of the month');
  });

  it('posts only the monthly fields and returns to the list', async () => {
    await create({ itemType: 1, periodId: String(PERIOD.monthly) });
    form().patchValue({ name: 'Side Gig', amount: 250, monthlyDom: 15, weeklyDow: 3 });

    await submit();

    const req = http.expectOne(apiUrl('/items'));
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toMatchObject({ name: 'Side Gig', amount: 250, fkItemType: 1, fkPeriod: 6, monthlyDom: 15, weeklyDow: null });
    req.flush(envelope(makeItem(PERIOD.monthly, { id: 9, name: 'Side Gig' }), 201, 'Item created successfully'));
    await fixture.whenStable();

    expect(navigated).toEqual(['/credits']);
  });

  it('shows the first broken rule instead of sending a request', async () => {
    await create({ itemType: 2, periodId: String(PERIOD.biMonthly) });
    form().patchValue({ name: 'Rent', amount: 1500, biMonthlyDay1: 1, biMonthlyDay2: 1 });

    await submit();

    expect(el().textContent).toContain('The two days must be different.');
  });

  it('loads an existing item for editing and saves with PUT', async () => {
    await create({ itemType: 2, id: '7' });

    http.expectOne(apiUrl('/items/7')).flush(
      envelope(makeItem(PERIOD.weekly, { id: 7, name: 'Groceries', amount: 120, weeklyDow: 6, itemType: { id: 2, name: 'Debit' } })),
    );
    await fixture.whenStable();

    expect(el().textContent).toContain('Edit Debit');
    expect(form().getRawValue()).toMatchObject({ name: 'Groceries', amount: 120, periodId: PERIOD.weekly, weeklyDow: 6 });

    form().patchValue({ amount: 140 });
    await submit();

    const req = http.expectOne(apiUrl('/items/7'));
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toMatchObject({ amount: 140, fkItemType: 2, fkPeriod: PERIOD.weekly, weeklyDow: 6 });
    req.flush(envelope(makeItem(PERIOD.weekly, { id: 7 }), 200, 'Item updated successfully'));
    await fixture.whenStable();

    expect(navigated).toEqual(['/debits']);
  });

  it('shows the API message when the save is rejected', async () => {
    await create({ itemType: 1, periodId: String(PERIOD.daily) });
    form().patchValue({ name: 'Coffee', amount: 5 });

    await submit();
    http
      .expectOne(apiUrl('/items'))
      .flush(envelope(null, 400, 'The item type or time period does not exist'), { status: 400, statusText: 'Bad Request' });
    await fixture.whenStable();

    expect(el().textContent).toContain('The item type or time period does not exist');
    expect(navigated).toEqual([]);
  });
});

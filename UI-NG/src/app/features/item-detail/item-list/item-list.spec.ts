import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, authResponse, envelope } from '../../../../testing/test-providers';
import { apiUrl } from '../../../core/api/api-response';
import { toSession } from '../../../core/auth/auth.models';
import { PERIOD } from '../item';
import { makeItem } from '../testing-items';
import { ItemList } from './item-list';

describe('ItemList', () => {
  let fixture: ComponentFixture<ItemList>;
  let http: HttpTestingController;
  const userId = authResponse().userId;

  async function create(itemType: number) {
    sessionStorage.clear();
    sessionStorage.setItem('fpfl.session', JSON.stringify(toSession(authResponse())));
    await TestBed.configureTestingModule({
      imports: [ItemList],
      providers: appTestProviders(),
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ItemList);
    fixture.componentRef.setInput('itemType', itemType);
    fixture.detectChanges();
    TestBed.tick(); // lets httpResource send its request
  }

  afterEach(() => http.verify());

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists credits with amount, period, schedule and dates', async () => {
    await create(1);

    http.expectOne(apiUrl(`/items/${userId}/1`)).flush(
      envelope([
        makeItem(PERIOD.biMonthly, { id: 1, name: 'Salary', amount: 2500, biMonthlyDay1: 1, biMonthlyDay2: 15 }),
        makeItem(PERIOD.oneTime, { id: 2, name: 'Tax Refund', amount: 1234.5, beginDate: '2026-04-15' }),
      ]),
    );
    await fixture.whenStable();

    expect(text()).toContain('Credits');
    expect(text()).toContain('Salary');
    expect(text()).toContain('$2,500.00');
    expect(text()).toContain('On the 1st and 15th');
    expect(text()).toContain('Tax Refund');
    expect(text()).toContain('$1,234.50');
    expect(text()).toContain('Apr 15, 2026');
    expect(text()).toContain('2 items');
  });

  it('asks for debits when the route says itemType 2', async () => {
    await create(2);

    http.expectOne(apiUrl(`/items/${userId}/2`)).flush(envelope([]));
    await fixture.whenStable();

    expect(text()).toContain('Debits');
    expect(text()).toContain('No debits yet.');
  });

  it('links New to the period picker and Edit to the item', async () => {
    await create(1);
    http.expectOne(apiUrl(`/items/${userId}/1`)).flush(envelope([makeItem(PERIOD.daily, { id: 3, name: 'Coffee' })]));
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('New Credit');
    expect(el.querySelector('[aria-label="Edit Coffee"]')).toBeTruthy();
  });

  it('deletes an item after confirmation and reloads the list', async () => {
    await create(2);
    http.expectOne(apiUrl(`/items/${userId}/2`)).flush(envelope([makeItem(PERIOD.monthly, { id: 4, name: 'Rent', amount: 1500 })]));
    await fixture.whenStable();

    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[aria-label="Delete Rent"] button, button[aria-label="Delete Rent"]')!.click();
    await fixture.whenStable();
    expect(document.body.textContent).toContain('This can\'t be undone.');

    const confirm = Array.from(document.body.querySelectorAll<HTMLButtonElement>('button')).find(
      (button) => button.textContent?.trim() === 'Delete',
    )!;
    confirm.click();

    const req = http.expectOne(apiUrl('/items/4'));
    expect(req.request.method).toBe('DELETE');
    req.flush(envelope(null, 200, 'Item 4 deleted successfully'));

    TestBed.tick();
    http.expectOne(apiUrl(`/items/${userId}/2`)).flush(envelope([]));
    await fixture.whenStable();

    expect(text()).toContain('No debits yet.');
  });

  it('filters rows by any word in the name, period or schedule', async () => {
    await create(1);
    http.expectOne(apiUrl(`/items/${userId}/1`)).flush(
      envelope([
        makeItem(PERIOD.biMonthly, { id: 1, name: 'Salary', biMonthlyDay1: 1, biMonthlyDay2: 15 }),
        makeItem(PERIOD.oneTime, { id: 2, name: 'Tax Refund', beginDate: '2026-04-15' }),
        makeItem(PERIOD.monthly, { id: 3, name: 'Side Gig', monthlyDom: 15 }),
      ]),
    );
    await fixture.whenStable();

    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[type="search"]')!;
    const type = async (value: string) => {
      input.value = value;
      input.dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };

    await type('refund');
    expect(text()).toContain('Tax Refund');
    expect(text()).not.toContain('Salary');
    expect(text()).toContain('1 of 3 items');

    await type('SALARY 15th');
    expect(text()).toContain('Salary');
    expect(text()).not.toContain('Side Gig');

    await type('nothing like this');
    expect(text()).toContain('No credits match “nothing like this”.');
  });

  it('shows the API error message and reference', async () => {
    await create(1);

    http
      .expectOne(apiUrl(`/items/${userId}/1`))
      .flush(envelope(null, 403, 'You can only view your own items', 'ref-9'), { status: 403, statusText: 'Forbidden' });
    await fixture.whenStable();

    expect(text()).toContain('You can only view your own items');
    expect(text()).toContain('ref-9');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';

import { appTestProviders } from '../../../../testing/test-providers';
import { PeriodPicker } from './period-picker';

describe('PeriodPicker', () => {
  let fixture: ComponentFixture<PeriodPicker>;

  async function create(itemType: number) {
    await TestBed.configureTestingModule({ imports: [PeriodPicker], providers: appTestProviders() }).compileComponents();
    fixture = TestBed.createComponent(PeriodPicker);
    fixture.componentRef.setInput('itemType', itemType);
    await fixture.whenStable();
  }

  const links = () => Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('.period'));

  it('offers all ten periods, each linking to the form for that period', async () => {
    await create(1);

    expect(links()).toHaveLength(10);
    expect(links()[5].textContent).toContain('Monthly');
    expect(links()[5].getAttribute('href')).toBe('/credits/new/6');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('New Credit');
  });

  it('targets the debits pages for debits', async () => {
    await create(2);

    expect(links()[0].getAttribute('href')).toBe('/debits/new/1');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('New Debit');
  });
});

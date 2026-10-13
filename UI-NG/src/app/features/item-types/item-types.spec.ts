import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { appTestProviders, envelope } from '../../../testing/test-providers';
import { apiUrl } from '../../core/api/api-response';
import { ItemTypes } from './item-types';

describe('ItemTypes', () => {
  let fixture: ComponentFixture<ItemTypes>;
  let http: HttpTestingController;

  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [ItemTypes],
      providers: appTestProviders(),
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ItemTypes);
    fixture.detectChanges();
    TestBed.tick(); // lets httpResource send its request
  });

  afterEach(() => http.verify());

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists the item types returned by the API', async () => {
    http.expectOne(apiUrl('/item-types')).flush(
      envelope([
        { id: 1, name: 'Credit' },
        { id: 2, name: 'Debit' },
        { id: 3, name: 'Initial Amount' },
      ]),
    );
    await fixture.whenStable();

    expect(text()).toContain('Credit');
    expect(text()).toContain('Debit');
    expect(text()).toContain('Initial Amount');
  });

  it('shows the API error message and reference', async () => {
    http
      .expectOne(apiUrl('/item-types'))
      .flush(envelope(null, 500, 'An unexpected error occurred.', 'ref-42'), {
        status: 500,
        statusText: 'Server Error',
      });
    await fixture.whenStable();

    expect(text()).toContain('An unexpected error occurred.');
    expect(text()).toContain('ref-42');
  });
  it('hides the admin actions from non-admins', async () => {
    http.expectOne(apiUrl('/item-types')).flush(envelope([{ id: 1, name: 'Credit' }]));
    await fixture.whenStable();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('[aria-label="Edit Credit"]')).toBeNull();
    expect(el.textContent).not.toContain('Add');
  });
});

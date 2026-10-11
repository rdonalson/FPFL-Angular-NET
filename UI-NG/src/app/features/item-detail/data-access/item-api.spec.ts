import { TestBed } from '@angular/core/testing';
import { ItemApi } from './item-api';

describe('ItemApi', () => {
  let service: ItemApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ItemApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

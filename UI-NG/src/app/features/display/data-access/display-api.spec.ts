import { TestBed } from '@angular/core/testing';
import { DisplayApi } from './display-api';

describe('DisplayApi', () => {
  let service: DisplayApi;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DisplayApi);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

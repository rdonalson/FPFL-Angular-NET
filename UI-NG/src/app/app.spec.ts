import { TestBed } from '@angular/core/testing';

import { appTestProviders } from '../testing/test-providers';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: appTestProviders(),
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the shell with the app title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('app-topbar')?.textContent).toContain(
      'Financial Planner Forecast Ledger',
    );
    expect(compiled.querySelector('app-footer')).toBeTruthy();
  });
});

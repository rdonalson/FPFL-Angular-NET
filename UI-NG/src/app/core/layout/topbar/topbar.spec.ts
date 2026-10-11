import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Topbar } from './topbar';

describe('Topbar', () => {
  let fixture: ComponentFixture<Topbar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Topbar],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(Topbar);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should link to the GitHub repo', () => {
    const link = (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>(
      '.topbar__repo',
    );
    expect(link?.href).toContain('github.com/rdonalson/FPFL-Angular-NET');
  });
});

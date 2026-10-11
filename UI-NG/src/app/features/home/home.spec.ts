import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GALLERY_IMAGES } from './gallery-images';
import { Home } from './home';

describe('Home', () => {
  let fixture: ComponentFixture<Home>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Home],
    }).compileComponents();

    fixture = TestBed.createComponent(Home);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the galleria and the guide link', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('p-galleria')).toBeTruthy();
    expect(el.querySelector<HTMLAnchorElement>('.home__guide')?.getAttribute('href')).toBe(
      'media/financial-planner-guide.pdf',
    );
  });
});

describe('GALLERY_IMAGES', () => {
  it('builds readable alt text from file names', () => {
    const image = GALLERY_IMAGES.find((i) => i.src.includes('DavesCreekDr'));
    expect(image?.alt).toBe('Daves Creek Dr at Daves Creek Rd');
  });

  it('URL-encodes file names that contain @', () => {
    expect(GALLERY_IMAGES.every((i) => !i.src.includes('@'))).toBe(true);
  });
});

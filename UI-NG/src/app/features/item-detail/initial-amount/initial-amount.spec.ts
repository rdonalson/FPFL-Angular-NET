import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InitialAmount } from './initial-amount';

describe('InitialAmount', () => {
  let component: InitialAmount;
  let fixture: ComponentFixture<InitialAmount>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InitialAmount],
    }).compileComponents();

    fixture = TestBed.createComponent(InitialAmount);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

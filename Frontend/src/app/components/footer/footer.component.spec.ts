import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FooterComponent } from './footer.component';
import { RouterTestingModule } from '@angular/router/testing';

describe('FooterComponent', () => {
  let fixture: ComponentFixture<FooterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [FooterComponent, RouterTestingModule] }).compileComponents();
    fixture = TestBed.createComponent(FooterComponent);
    fixture.detectChanges();
  });

  it('should create', () => expect(fixture.componentInstance).toBeTruthy());
  it('should display current year', () => expect(fixture.nativeElement.querySelector('.footer-copy').textContent).toContain(new Date().getFullYear().toString()));
  it('should render brand', () => expect(fixture.nativeElement.querySelector('.footer-brand').textContent).toContain('inkwell'));
  it('should have 3 nav links', () => expect(fixture.nativeElement.querySelectorAll('.footer-link').length).toBe(3));
});

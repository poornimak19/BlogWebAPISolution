import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResetPasswordComponent } from './reset-password.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';

describe('ResetPasswordComponent', () => {
  let fixture: ComponentFixture<ResetPasswordComponent>;
  let comp: ResetPasswordComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResetPasswordComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(ResetPasswordComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should not be done initially', () => expect(comp.done()).toBeFalse());
  it('should not be submitting initially', () => expect(comp.submitting()).toBeFalse());

  it('should show error for short password', () => {
    comp.password = '123';
    comp.submit();
    expect(comp.error()).toContain('6 characters');
  });

  it('should show error when passwords do not match', () => {
    comp.password = 'abc123';
    comp.confirm  = 'xyz789';
    comp.submit();
    expect(comp.error()).toBe('Passwords do not match.');
  });

  it('should show error when token is missing', () => {
    comp.password = 'abc123';
    comp.confirm  = 'abc123';
    comp.submit();
    expect(comp.error()).toBeTruthy();
  });

  it('should show form when not done', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.rp-form')).toBeTruthy();
  });

  it('should show success block when done', () => {
    comp.done.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.rp-success')).toBeTruthy();
  });
});

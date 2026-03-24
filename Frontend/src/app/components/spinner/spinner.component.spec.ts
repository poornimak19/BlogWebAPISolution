import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SpinnerComponent } from './spinner.component';
import { LoadingService } from '../../services/ui.services';

describe('SpinnerComponent', () => {
  let fixture: ComponentFixture<SpinnerComponent>;
  let loadingSvc: LoadingService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SpinnerComponent] }).compileComponents();
    fixture   = TestBed.createComponent(SpinnerComponent);
    loadingSvc = TestBed.inject(LoadingService);
    fixture.detectChanges();
  });

  it('should create', () => expect(fixture.componentInstance).toBeTruthy());

  it('should be hidden when not loading', () => {
    expect(fixture.nativeElement.querySelector('.spinner-overlay')).toBeNull();
  });

  it('should be visible when loading', () => {
    loadingSvc.show();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.spinner-overlay')).toBeTruthy();
  });

  it('should hide after loading stops', () => {
    loadingSvc.show();
    fixture.detectChanges();
    loadingSvc.hide();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.spinner-overlay')).toBeNull();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastComponent } from './toast.component';
import { ToastService } from '../../services/ui.services';

describe('ToastComponent', () => {
  let fixture: ComponentFixture<ToastComponent>;
  let svc: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ToastComponent] }).compileComponents();
    fixture = TestBed.createComponent(ToastComponent);
    svc     = TestBed.inject(ToastService);
    fixture.detectChanges();
  });

  it('should create', () => expect(fixture.componentInstance).toBeTruthy());

  it('should show a success toast', () => {
    svc.success('Done!');
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('.toast--success');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('Done!');
  });

  it('should show an error toast', () => {
    svc.error('Oops');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.toast--error')).toBeTruthy();
  });

  it('iconFor() returns correct icons', () => {
    const c = fixture.componentInstance;
    expect(c.iconFor('success')).toBe('✓');
    expect(c.iconFor('error')).toBe('✕');
    expect(c.iconFor('info')).toBe('ℹ');
    expect(c.iconFor('warning')).toBe('⚠');
  });

  it('should remove toast on click', () => {
    svc.info('Hello');
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('.toast') as HTMLElement).click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.toast')).toBeNull();
  });
});

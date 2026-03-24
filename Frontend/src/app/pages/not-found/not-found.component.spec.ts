import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotFoundComponent } from './not-found.component';
import { RouterTestingModule } from '@angular/router/testing';

describe('NotFoundComponent', () => {
  let fixture: ComponentFixture<NotFoundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [NotFoundComponent, RouterTestingModule] }).compileComponents();
    fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();
  });

  it('should create', () => expect(fixture.componentInstance).toBeTruthy());
  it('should show 404', () => expect(fixture.nativeElement.querySelector('.nf-number').textContent).toContain('404'));
  it('should have a back-to-home link', () => expect(fixture.nativeElement.querySelector('.nf-btn')).toBeTruthy());
});

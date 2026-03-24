import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NavbarComponent } from './navbar.component';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('NavbarComponent', () => {
  let fixture: ComponentFixture<NavbarComponent>;
  let comp: NavbarComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavbarComponent, RouterTestingModule, HttpClientTestingModule]
    }).compileComponents();
    fixture = TestBed.createComponent(NavbarComponent);
    comp    = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should show Sign In button when guest', () => expect(fixture.nativeElement.querySelector('.btn-signin')).toBeTruthy());
  it('should show Guest message when not logged in', () => expect(fixture.nativeElement.querySelector('.nav-guest')).toBeTruthy());
  it('should toggle menu', () => { expect(comp.menuOpen()).toBeFalse(); comp.toggleMenu(); expect(comp.menuOpen()).toBeTrue(); });
  it('should close menu', () => { comp.menuOpen.set(true); comp.closeMenu(); expect(comp.menuOpen()).toBeFalse(); });
  it('avatarLetter should return ? when no user', () => expect(comp.avatarLetter).toBe('?'));
});

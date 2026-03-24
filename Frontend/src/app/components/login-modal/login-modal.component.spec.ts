import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginModalComponent } from './login-modal.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { LoginModalService } from '../../services/ui.services';

describe('LoginModalComponent', () => {
  let fixture: ComponentFixture<LoginModalComponent>;
  let comp: LoginModalComponent;
  let modalSvc: LoginModalService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginModalComponent, HttpClientTestingModule, RouterTestingModule]
    }).compileComponents();
    fixture  = TestBed.createComponent(LoginModalComponent);
    comp     = fixture.componentInstance;
    modalSvc = TestBed.inject(LoginModalService);
    fixture.detectChanges();
  });

  it('should create', () => expect(comp).toBeTruthy());
  it('should be hidden by default', () => expect(fixture.nativeElement.querySelector('.modal-box')).toBeNull());
  it('should show when opened', () => { modalSvc.open(); fixture.detectChanges(); expect(fixture.nativeElement.querySelector('.modal-box')).toBeTruthy(); });
  it('should switch tabs', () => { comp.switchTab('register'); expect(comp.tab()).toBe('register'); });
  it('should clear error on tab switch', () => { comp.error.set('err'); comp.switchTab('login'); expect(comp.error()).toBe(''); });
  it('should require login fields', () => { comp.login(); expect(comp.error()).toBeTruthy(); });
  it('should require register fields', () => { comp.register(); expect(comp.error()).toBeTruthy(); });
  it('should require email for forgot', () => { comp.forgotPassword(); expect(comp.error()).toBeTruthy(); });
  it('should validate short password on register', () => { comp.registerData = { email: 'a@b.com', username: 'user', password: '123', displayName: '', role: 'Reader' }; comp.register(); expect(comp.error()).toContain('6 characters'); });
});

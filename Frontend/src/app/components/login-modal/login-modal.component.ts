import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ToastService } from '../../services/ui.services';

type Tab = 'login' | 'register' | 'forgot';

@Component({
  selector: 'app-login-modal',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './login-modal.component.html',
  styleUrls: ['./login-modal.component.css']
})
export class LoginModalComponent {
  readonly modal = inject(LoginModalService);
  readonly auth  = inject(AuthService);
  readonly toast = inject(ToastService);

  tab        = signal<Tab>('login');
  busy       = signal(false);
  error      = signal('');
  forgotSent = signal(false);

  loginData    = { emailOrUsername: '', password: '' };
  registerData = { email: '', username: '', password: '', displayName: '', role: 'Reader' as 'Reader' | 'Blogger' };
  forgotEmail  = '';

  switchTab(t: Tab): void { this.tab.set(t); this.error.set(''); this.forgotSent.set(false); }

  onOverlay(e: MouseEvent): void {
    if ((e.target as HTMLElement).classList.contains('modal-overlay')) this.modal.close();
  } //handles click outside the model

  login(): void {
    if (!this.loginData.emailOrUsername || !this.loginData.password) {
      this.error.set('Please fill in all fields.'); return;
    }
    this.busy.set(true); this.error.set('');
    this.auth.login({ emailOrUsername: this.loginData.emailOrUsername, password: this.loginData.password }).subscribe({
      next: () => {
        this.toast.success('Welcome back!');
        this.modal.close();
        this.busy.set(false);
        this.loginData = { emailOrUsername: '', password: '' };
      },
      error: e => { this.error.set(e.error?.message || 'Invalid credentials.'); this.busy.set(false); }
    });
  }

  register(): void {
    const d = this.registerData;
    if (!d.email || !d.username || !d.password) { this.error.set('Please fill in all required fields.'); return; }
    if (d.password.length < 6) { this.error.set('Password must be at least 6 characters.'); return; }
    this.busy.set(true); this.error.set('');
    this.auth.register({ email: d.email, username: d.username, password: d.password, displayName: d.displayName || undefined, role: d.role }).subscribe({
      next: () => { this.toast.success('Account created! Welcome!'); this.modal.close(); this.busy.set(false); },
      error: e => { this.error.set(e.error?.message || 'Enter correct mail ID.'); this.busy.set(false); }
    });
  }

  forgotPassword(): void {
    if (!this.forgotEmail) { this.error.set('Please enter your email.'); return; }
    this.busy.set(true); this.error.set('');
    this.auth.forgotPassword({ email: this.forgotEmail }).subscribe({
      next: () => { this.forgotSent.set(true); this.busy.set(false); },
      error: () => { this.forgotSent.set(true); this.busy.set(false); }
    });
  }
}

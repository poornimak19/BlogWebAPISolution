import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/ui.services';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent {
  readonly route  = inject(ActivatedRoute);
  readonly auth   = inject(AuthService);
  readonly toast  = inject(ToastService);
  readonly router = inject(Router);

  password   = '';
  confirm    = '';
  submitting = signal(false);
  error      = signal('');
  done       = signal(false);

  submit(): void {
    this.error.set('');
    if (this.password.length < 6) { this.error.set('Password must be at least 6 characters.'); return; }
    if (this.password !== this.confirm) { this.error.set('Passwords do not match.'); return; }
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) { this.error.set('Missing reset token.'); return; }
    this.submitting.set(true);
    this.auth.resetPassword({ token, newPassword: this.password }).subscribe({
      next: () => { this.done.set(true); this.submitting.set(false); },
      error: e => { this.error.set(e.error?.message || 'Reset failed. Token may have expired.'); this.submitting.set(false); }
    });
  }
}

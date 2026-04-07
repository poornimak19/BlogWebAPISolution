import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { PremiumService } from '../../services/premium.service';
import { LoginModalService, ToastService } from '../../services/ui.services';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-premium',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule],
  templateUrl: './premium.component.html',
  styleUrls: ['./premium.component.css']
})
export class PremiumComponent implements OnInit {
  readonly auth        = inject(AuthService);
  readonly premiumSvc  = inject(PremiumService);
  readonly toast       = inject(ToastService);
  readonly loginModal  = inject(LoginModalService);
  readonly router      = inject(Router);

  step        = signal<'plans' | 'payment' | 'success'>('plans');
  processing  = signal(false);

  // Simulated card form fields
  card = { name: '', number: '', expiry: '', cvv: '' };

  isAlreadyPremium = computed(() => {
    const u = this.auth.currentUser();
    if (!u?.isPremiumSubscriber) return false;
    if (!u.premiumExpiresAt) return true;
    return new Date(u.premiumExpiresAt) > new Date();
  });

  expiresAt = computed(() => {
    const u = this.auth.currentUser();
    return u?.premiumExpiresAt ? new Date(u.premiumExpiresAt) : null;
  });

  ngOnInit(): void {
    if (this.isAlreadyPremium()) this.step.set('success');
  }

  goToPayment(): void {
    if (!this.auth.isLoggedIn()) { this.loginModal.open(); return; }
    this.step.set('payment');
  }

  pay(): void {
    // Basic validation
    if (!this.card.name.trim())   { this.toast.error('Enter cardholder name.'); return; }
    if (this.card.number.replace(/\s/g, '').length < 16) { this.toast.error('Enter a valid 16-digit card number.'); return; }
    if (!this.card.expiry.match(/^\d{2}\/\d{2}$/)) { this.toast.error('Enter expiry as MM/YY.'); return; }
    if (this.card.cvv.length < 3) { this.toast.error('Enter a valid CVV.'); return; }

    this.processing.set(true);
    // Simulate 1.5s payment processing then call backend
    setTimeout(() => {
      this.premiumSvc.subscribe().subscribe({
        next: () => {
          this.auth.fetchMe().subscribe();
          this.step.set('success');
          this.processing.set(false);
        },
        error: () => {
          this.toast.error('Payment failed. Please try again.');
          this.processing.set(false);
        }
      });
    }, 1500);
  }

  formatCardNumber(e: Event): void {
    const input = e.target as HTMLInputElement;
    let v = input.value.replace(/\D/g, '').slice(0, 16);
    this.card.number = v.replace(/(.{4})/g, '$1 ').trim();
    input.value = this.card.number;
  }

  formatExpiry(e: Event): void {
    const input = e.target as HTMLInputElement;
    let v = input.value.replace(/\D/g, '').slice(0, 4);
    if (v.length >= 3) v = v.slice(0, 2) + '/' + v.slice(2);
    this.card.expiry = v;
    input.value = v;
  }
}

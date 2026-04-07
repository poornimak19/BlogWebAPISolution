import { Component, inject, signal, HostListener, ElementRef, computed } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ThemeService } from '../../services/ui.services';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  readonly auth       = inject(AuthService);
  readonly loginModal = inject(LoginModalService);
  readonly theme      = inject(ThemeService);
  private  router     = inject(Router);
  private  elRef      = inject(ElementRef);

  menuOpen = signal(false);
  scrolled = signal(false);

  isPremium = computed(() => {
    const u = this.auth.currentUser();
    if (!u?.isPremiumSubscriber) return false;
    if (!u.premiumExpiresAt) return true;
    return new Date(u.premiumExpiresAt) > new Date();
  });

  /** Close dropdown when clicking anywhere OUTSIDE the navbar */
  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    if (!this.elRef.nativeElement.contains(e.target as Node)) {
      this.menuOpen.set(false);
    }
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrolled.set(window.scrollY > 10);
  }

  get displayName(): string {
    const u = this.auth.currentUser();
    return u?.displayName || u?.username || '';
  }

  get avatarLetter(): string {
    return this.displayName[0]?.toUpperCase() ?? '?';
  }

  get avatarUrl(): string | undefined {
    const url = this.auth.currentUser()?.avatarUrl;
    return url && url.trim() ? url : undefined;
  }

  /** Toggle open/close; stop propagation so document:click doesn't immediately close it */
  toggleMenu(e: MouseEvent): void {
    e.stopPropagation();
    this.menuOpen.update(v => !v);
  }

  closeMenu(): void { this.menuOpen.set(false); }
  logout(): void    { this.auth.logout(); this.menuOpen.set(false); }

  onSearch(e: Event): void {
    const q = (e.target as HTMLInputElement).value.trim();
    if (q) {
      this.router.navigate(['/search'], { queryParams: { q } });
      (e.target as HTMLInputElement).value = '';
    }
  }
}

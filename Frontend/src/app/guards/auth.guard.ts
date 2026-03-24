import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { LoginModalService } from '../services/ui.services';

export const authGuard: CanActivateFn = () => {
  const auth  = inject(AuthService);
  const modal = inject(LoginModalService);
  if (auth.isLoggedIn()) return true;
  modal.open();
  return false;
};

export const bloggerGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  const role   = auth.userRole();
  if (role === 'Blogger' || role === 'Admin') return true;
  router.navigate(['/']);
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.userRole() === 'Admin') return true;
  router.navigate(['/']);
  return false;
};

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, finalize, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { LoadingService, ToastService, LoginModalService } from '../services/ui.services';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth    = inject(AuthService);
  const loading = inject(LoadingService);
  const toast   = inject(ToastService);
  const modal   = inject(LoginModalService);

  loading.show();

  const token  = auth.getToken();
  const cloned = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(cloned).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) { auth.logout(); modal.open(); }
      else if (err.status === 403) toast.error('Access denied.');
      else if (err.status === 500) toast.error('Server error. Please try again.');
      return throwError(() => err);
    }),
    finalize(() => loading.hide())
  );
};

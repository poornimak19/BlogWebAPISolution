import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PremiumAccessDto {
  fullAccess: boolean;
  isPremiumSubscriber: boolean;
  readsThisMonth: number;
  previewChars: number;
}

@Injectable({ providedIn: 'root' })
export class PremiumService {
  private readonly base = `${environment.apiUrl}/premium`;

  constructor(private http: HttpClient) {}

  checkAccess(postId: string): Observable<PremiumAccessDto> {
    return this.http.get<PremiumAccessDto>(`${this.base}/access/${postId}`)
      .pipe(catchError(e => throwError(() => e)));
  }

  subscribe(): Observable<{ message: string; expiresAt: string }> {
    return this.http.post<{ message: string; expiresAt: string }>(`${this.base}/subscribe`, {})
      .pipe(catchError(e => throwError(() => e)));
  }
}

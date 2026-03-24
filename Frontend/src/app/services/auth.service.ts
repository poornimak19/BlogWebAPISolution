import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import {
  RegisterRequestDto, LoginRequestDto, AuthResponseDto,
  MeResponseDto, ForgotPasswordRequestDto, ResetPasswordRequestDto
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = `${environment.apiUrl}/auth`;
  private _user = signal<MeResponseDto | null>(null);

  readonly currentUser = this._user.asReadonly();
  readonly isLoggedIn  = computed(() => !!this._user());
  readonly userRole    = computed(() => this._user()?.role ?? null);

  constructor(private http: HttpClient, private router: Router) {
    const token = localStorage.getItem('token');
    const user  = localStorage.getItem('user');
    if (token && user) {
      try { this._user.set(JSON.parse(user)); } catch { this._clear(); }
    }
  }

  register(dto: RegisterRequestDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.base}/register`, dto)
      .pipe(tap(r => this._onAuth(r)), catchError(e => throwError(() => e)));
  }

  login(dto: LoginRequestDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.base}/login`, dto)
      .pipe(tap(r => this._onAuth(r)), catchError(e => throwError(() => e)));
  }

  private _onAuth(res: AuthResponseDto): void {
    localStorage.setItem('token', res.token);
    this.fetchMe().subscribe();
  }

  fetchMe(): Observable<MeResponseDto> {
    return this.http.get<MeResponseDto>(`${this.base}/me`).pipe(
      tap(u => { this._user.set(u); localStorage.setItem('user', JSON.stringify(u)); }),
      catchError(e => { this._clear(); return throwError(() => e); })
    );
  }

  forgotPassword(dto: ForgotPasswordRequestDto): Observable<any> {
    return this.http.post(`${this.base}/forgot-password`, dto);
  }

  resetPassword(dto: ResetPasswordRequestDto): Observable<any> {
    return this.http.post(`${this.base}/reset-password`, dto);
  }

  logout(): void { this._clear(); this.router.navigate(['/']); }
  getToken(): string | null { return localStorage.getItem('token'); }

  private _clear(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this._user.set(null);
  }
}

import { Injectable, signal, effect } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count = 0;
  private _loading = signal(false);
  readonly loading = this._loading.asReadonly();
  show(): void { this._count++; this._loading.set(true); }
  hide(): void { this._count = Math.max(0, this._count - 1); if (this._count === 0) this._loading.set(false); }
}

export interface Toast { id: number; message: string; type: 'success' | 'error' | 'info' | 'warning'; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();
  private _id = 0;
  show(message: string, type: Toast['type'] = 'info', ms = 3500): void {
    const id = ++this._id;
    this._toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.remove(id), ms);
  }
  success(m: string) { this.show(m, 'success'); }
  error(m: string)   { this.show(m, 'error'); }
  info(m: string)    { this.show(m, 'info'); }
  warning(m: string) { this.show(m, 'warning'); }
  remove(id: number) { this._toasts.update(t => t.filter(x => x.id !== id)); }
}

@Injectable({ providedIn: 'root' })
export class LoginModalService {
  private _visible = signal(false);
  readonly visible = this._visible.asReadonly();
  open()  { this._visible.set(true); }
  close() { this._visible.set(false); }
}

// ── Theme Service ─────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _dark = signal<boolean>(false);
  readonly isDark = this._dark.asReadonly();

  constructor() {
    const saved = localStorage.getItem('inkwell-theme');
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const startDark = saved ? saved === 'dark' : prefersDark;
    this._dark.set(startDark);
    this._applyTheme(startDark);

    effect(() => {
      this._applyTheme(this._dark());
      localStorage.setItem('inkwell-theme', this._dark() ? 'dark' : 'light');
    });
  }

  toggle(): void { this._dark.update(v => !v); }

  private _applyTheme(dark: boolean): void {
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
  }
}

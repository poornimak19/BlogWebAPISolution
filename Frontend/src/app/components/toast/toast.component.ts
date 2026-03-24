import { Component, inject } from '@angular/core';
import { ToastService, Toast } from '../../services/ui.services';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [],
  templateUrl: './toast.component.html',
  styleUrls: ['./toast.component.css']
})
export class ToastComponent {
  readonly toastSvc = inject(ToastService);

  iconFor(type: Toast['type']): string {
    return ({ success: '✓', error: '✕', info: 'ℹ', warning: '⚠' })[type];
  }

  trackById(_: number, t: Toast): number { return t.id; }
}

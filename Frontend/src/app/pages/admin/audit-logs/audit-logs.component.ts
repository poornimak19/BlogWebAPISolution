import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, SlicePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { AuditLogDto } from '../../../models/audit-log.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [DatePipe, SlicePipe, FormsModule, AdminNavComponent],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.css']
})
export class AuditLogsComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);

  logs    = signal<AuditLogDto[]>([]);
  loading = signal(true);
  total   = signal(0);
  page    = signal(1);
  pageSize = 20;

  // Filters
  filterAction     = '';
  filterEntity     = '';
  filterStatus     = '';
  filterFrom       = '';
  filterTo         = '';

  expandedId = signal<string | null>(null);

  readonly actions   = ['', 'Create', 'Update', 'Delete', 'Login', 'Logout', 'Register', 'ForgotPassword', 'ResetPassword'];
  readonly entities  = ['', 'Post', 'Comment', 'User', 'Tag', 'Category', 'PostLike', 'CommentLike', 'Follow'];
  readonly statuses  = ['', 'Success', 'Failed'];
  readonly pageSizes = [10, 20, 50, 100];

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getAuditLogs({
      page:       this.page(),
      pageSize:   this.pageSize,
      action:     this.filterAction   || undefined,
      entityName: this.filterEntity   || undefined,
      status:     this.filterStatus   || undefined,
      from:       this.filterFrom     || undefined,
      to:         this.filterTo       || undefined,
    }).subscribe({
      next: r => { this.logs.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load audit logs.'); this.loading.set(false); }
    });
  }

  applyFilters(): void { this.page.set(1); this.load(); }
  resetFilters(): void {
    this.filterAction = ''; this.filterEntity = '';
    this.filterStatus = ''; this.filterFrom = ''; this.filterTo = '';
    this.page.set(1); this.load();
  }

  goTo(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page.set(p); this.load();
  }

  toggleExpand(id: string): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  get totalPages(): number { return Math.ceil(this.total() / this.pageSize); }

  roleClass(role: string): string {
    return ({ Admin: 'badge--danger', Blogger: 'badge--purple', Reader: 'badge--info' })[role] ?? 'badge--neutral';
  }

  actionClass(action: string): string {
    return ({
      Create: 'badge--success', Update: 'badge--info',
      Delete: 'badge--danger',  Login:  'badge--purple',
      Logout: 'badge--neutral', Register: 'badge--teal',
      ForgotPassword: 'badge--warning', ResetPassword: 'badge--warning'
    })[action] ?? 'badge--neutral';
  }

  statusClass(status: string): string {
    return status === 'Success' ? 'badge--success' : 'badge--danger';
  }

  formatJson(raw?: string): string {
    if (!raw) return '—';
    try { return JSON.stringify(JSON.parse(raw), null, 2); }
    catch { return raw; }
  }
}

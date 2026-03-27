import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { UserAdminDto } from '../../../models/admin.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [FormsModule, DatePipe, AdminNavComponent],
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.css']
})
export class AdminUsersComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);
  readonly auth     = inject(AuthService);

  users    = signal<UserAdminDto[]>([]);
  loading  = signal(true);
  search   = '';

  filtered = computed(() => {
    const q = this.search.toLowerCase();
    if (!q) return this.users();
    return this.users().filter(u =>
      u.username.toLowerCase().includes(q) ||
      u.email.toLowerCase().includes(q) ||
      (u.displayName?.toLowerCase().includes(q) ?? false)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getAllUsers().subscribe({
      next: u => { this.users.set(u); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load users.'); this.loading.set(false); }
    });
  }

  changeRole(user: UserAdminDto, role: string): void {
    if (!confirm(`Change ${user.username}'s role to ${role}?`)) return;
    this.adminSvc.changeRole(user.id, role).subscribe({
      next: () => { this.users.update(list => list.map(u => u.id === user.id ? { ...u, role } : u)); this.toast.success('Role updated.'); },
      error: e => this.toast.error(e.error?.message || 'Failed to change role.')
    });
  }

  toggleSuspend(user: UserAdminDto): void {
    const action = user.isSuspended ? 'unsuspend' : 'suspend';
    if (!confirm(`${action.charAt(0).toUpperCase() + action.slice(1)} ${user.username}?`)) return;
    this.adminSvc.suspendUser(user.id, !user.isSuspended).subscribe({
      next: () => { this.users.update(list => list.map(u => u.id === user.id ? { ...u, isSuspended: !u.isSuspended } : u)); this.toast.success(`User ${action}ed.`); },
      error: () => this.toast.error('Action failed.')
    });
  }

  toggleCommentBan(user: UserAdminDto): void {
    const action = user.canComment ? 'ban from commenting' : 'allow commenting';
    if (!confirm(`${action.charAt(0).toUpperCase() + action.slice(1)} for ${user.username}?`)) return;
    this.adminSvc.setCommentBan(user.id, !user.canComment).subscribe({
      next: () => { this.users.update(list => list.map(u => u.id === user.id ? { ...u, canComment: !u.canComment } : u)); this.toast.success('Comment permission updated.'); },
      error: () => this.toast.error('Action failed.')
    });
  }

  deleteUser(user: UserAdminDto): void {
    if (user.id === this.auth.currentUser()?.id) { this.toast.error("You can't delete yourself."); return; }
    if (!confirm(`Permanently delete ${user.username}? This cannot be undone.`)) return;
    this.adminSvc.deleteUser(user.id).subscribe({
      next: () => { this.users.update(list => list.filter(u => u.id !== user.id)); this.toast.success('User deleted.'); },
      error: () => this.toast.error('Failed to delete user.')
    });
  }

  roleBadgeClass(role: string): string {
    return ({ Admin: 'badge--admin', Blogger: 'badge--blogger', Reader: 'badge--reader' })[role] ?? 'badge--reader';
  }
}

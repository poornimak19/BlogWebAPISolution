import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { AdminPostDto } from '../../../models/admin.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';

@Component({
  selector: 'app-admin-posts',
  standalone: true,
  imports: [DatePipe, RouterLink, AdminNavComponent],
  templateUrl: './posts.component.html',
  styleUrls: ['./posts.component.css']
})
export class AdminPostsComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);

  posts   = signal<AdminPostDto[]>([]);
  loading = signal(true);
  page    = signal(1);
  total   = signal(0);
  readonly pageSize = 20;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getPendingPosts(this.page(), this.pageSize).subscribe({
      next: r => { this.posts.set(r.items as AdminPostDto[]); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load posts.'); this.loading.set(false); }
    });
  }

  approve(post: AdminPostDto): void {
    this.adminSvc.approvePost(post.id).subscribe({
      next: () => { this.posts.update(list => list.filter(p => p.id !== post.id)); this.total.update(v => v - 1); this.toast.success('Post approved.'); },
      error: () => this.toast.error('Failed to approve.')
    });
  }

  reject(post: AdminPostDto): void {
    if (!confirm(`Reject "${post.title}"?`)) return;
    this.adminSvc.rejectPost(post.id).subscribe({
      next: () => { this.posts.update(list => list.filter(p => p.id !== post.id)); this.total.update(v => v - 1); this.toast.success('Post rejected.'); },
      error: () => this.toast.error('Failed to reject.')
    });
  }

  deletePost(post: AdminPostDto): void {
    if (!confirm(`Permanently delete "${post.title}"?`)) return;
    this.adminSvc.adminDeletePost(post.id).subscribe({
      next: () => { this.posts.update(list => list.filter(p => p.id !== post.id)); this.total.update(v => v - 1); this.toast.success('Post deleted.'); },
      error: () => this.toast.error('Failed to delete.')
    });
  }

  statusClass(status: string): string {
    return ({ Published: 'badge--success', Draft: 'badge--neutral', Archived: 'badge--warning' })[status] ?? 'badge--neutral';
  }
}

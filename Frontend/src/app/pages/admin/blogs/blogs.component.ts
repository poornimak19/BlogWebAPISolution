import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { AdminPostDto } from '../../../models/admin.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';
import { environment } from '../../../../environments/environment';
import { PostDetailDto } from '../../../models/post.models';

const mediaBase = environment.apiUrl.replace('/api', '');

@Component({
  selector: 'app-admin-blogs',
  standalone: true,
  imports: [DatePipe, FormsModule, AdminNavComponent],
  templateUrl: './blogs.component.html',
  styleUrls: ['./blogs.component.css']
})
export class AdminBlogsComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);
  private readonly http = inject(HttpClient);

  posts   = signal<AdminPostDto[]>([]);
  loading = signal(true);
  page    = signal(1);
  total   = signal(0);

  query      = '';
  visibility = 'all';
  pageSize   = 20;
  readonly pageSizeOptions = [5,10, 20, 50];

  previewPost    = signal<PostDetailDto | null>(null);
  previewLoading = signal(false);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getAllPosts(this.page(), this.pageSize, this.query.trim() || undefined, this.visibility).subscribe({
      next: r => { this.posts.set(r.items as AdminPostDto[]); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load blogs.'); this.loading.set(false); }
    });
  }

  onVisibilityChange(): void { this.page.set(1); this.load(); }
  onPageSizeChange(): void   { this.page.set(1); this.load(); }
  search(): void             { this.page.set(1); this.load(); }

  openPreview(post: AdminPostDto): void {
    this.previewPost.set(null);
    this.previewLoading.set(true);
    this.http.get<PostDetailDto>(`${environment.apiUrl}/posts/slug/${post.slug}`).subscribe({
      next: p  => { this.previewPost.set(p); this.previewLoading.set(false); },
      error: () => { this.toast.error('Failed to load preview.'); this.previewLoading.set(false); }
    });
  }

  closePreview(): void { this.previewPost.set(null); }

  mediaUrl(url: string): string {
    return url.startsWith('http') ? url : `${mediaBase}${url}`;
  }

  deletePost(post: AdminPostDto): void {
    if (!confirm(`Permanently delete "${post.title}"?`)) return;
    this.adminSvc.adminDeletePost(post.id).subscribe({
      next: () => { this.posts.update(l => l.filter(p => p.id !== post.id)); this.total.update(v => v - 1); this.toast.success('Post deleted.'); },
      error: () => this.toast.error('Failed to delete.')
    });
  }

  get totalPages(): number { return Math.ceil(this.total() / this.pageSize); }

  goTo(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page.set(p);
    this.load();
  }

  statusClass(status: string): string {
    return ({ Published: 'badge--success', Draft: 'badge--neutral', Archived: 'badge--warning' })[status] ?? 'badge--neutral';
  }
}

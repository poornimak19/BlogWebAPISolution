import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { AdminCommentDto } from '../../../models/admin.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';

@Component({
  selector: 'app-admin-comments',
  standalone: true,
  imports: [DatePipe, AdminNavComponent],
  templateUrl: './comments.component.html',
  styleUrls: ['./comments.component.css']
})
export class AdminCommentsComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);

  comments = signal<AdminCommentDto[]>([]);
  loading  = signal(true);
  total    = signal(0);
  page     = signal(1);
  readonly pageSize = 20;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getPendingComments(this.page(), this.pageSize).subscribe({
      next: r => { this.comments.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load comments.'); this.loading.set(false); }
    });
  }

  approve(comment: AdminCommentDto): void {
    this.adminSvc.approveComment(comment.id).subscribe({
      next: () => {
        this.comments.update(list => list.filter(c => c.id !== comment.id));
        this.total.update(v => Math.max(0, v - 1));
        this.toast.success('Comment approved.');
      },
      error: () => this.toast.error('Failed to approve.')
    });
  }

  deleteComment(comment: AdminCommentDto): void {
    if (!confirm('Permanently delete this comment?')) return;
    this.adminSvc.adminDeleteComment(comment.id).subscribe({
      next: () => {
        this.comments.update(list => list.filter(c => c.id !== comment.id));
        this.total.update(v => Math.max(0, v - 1));
        this.toast.success('Comment deleted.');
      },
      error: () => this.toast.error('Failed to delete.')
    });
  }

  banCommenter(comment: AdminCommentDto): void {
    if (!comment.author) return;
    if (!confirm(`Ban ${comment.author.username} from commenting?`)) return;
    this.adminSvc.setCommentBan(comment.author.id, false).subscribe({
      next: () => this.toast.success(`${comment.author!.username} banned from commenting.`),
      error: () => this.toast.error('Failed to ban commenter.')
    });
  }

  truncate(text: string, max = 120): string {
    return text.length > max ? text.slice(0, max) + '…' : text;
  }
}

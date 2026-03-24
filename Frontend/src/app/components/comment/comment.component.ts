import { Component, Input, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommentDto, ThreadedComment } from '../../models/blog.models';
import { CommentService, ReactionService } from '../../services/blog.services';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ToastService } from '../../services/ui.services';

@Component({
  selector: 'app-comment',
  standalone: true,
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './comment.component.html',
  styleUrls: ['./comment.component.css']
})
export class CommentComponent implements OnInit {
  @Input({ required: true }) postId!: string;

  readonly auth        = inject(AuthService);
  readonly commentSvc  = inject(CommentService);
  readonly reactionSvc = inject(ReactionService);
  readonly loginModal  = inject(LoginModalService);
  readonly toast       = inject(ToastService);

  threads        = signal<ThreadedComment[]>([]);
  total          = signal(0);
  loading        = signal(false);
  submitting     = signal(false);
  composeFocused = signal(false);
  replyingTo     = signal<string | null>(null);
  likedSet       = new Set<string>();

  newText   = '';
  replyText = '';

  get avatarLetter(): string {
    const u = this.auth.currentUser();
    return (u?.displayName || u?.username || '?')[0].toUpperCase();
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.commentSvc.getThreaded(this.postId).subscribe({
      next: d => { this.threads.set(d); this.total.set(d.reduce((s, t) => s + 1 + t.replies.length, 0)); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  submit(): void {
    if (!this.newText.trim() || this.submitting()) return;
    this.submitting.set(true);
    this.commentSvc.add(this.postId, { content: this.newText.trim() }).subscribe({
      next: c => {
        this.threads.update(t => [{ parent: c, replies: [] }, ...t]);
        this.total.update(v => v + 1);
        this.newText = ''; this.composeFocused.set(false); this.submitting.set(false);
        this.toast.success('Response published!');
      },
      error: e => { this.toast.error(e.error?.message || 'Failed to post.'); this.submitting.set(false); }
    });
  }

  startReply(id: string): void {
    if (!this.auth.isLoggedIn()) { this.loginModal.open(); return; }
    this.replyingTo.set(this.replyingTo() === id ? null : id);
    this.replyText = '';
  }

  submitReply(parentId: string): void {
    if (!this.replyText.trim() || this.submitting()) return;
    this.submitting.set(true);
    this.commentSvc.add(this.postId, { content: this.replyText.trim(), parentCommentId: parentId }).subscribe({
      next: r => {
        this.threads.update(ts => ts.map(t => t.parent.id === parentId
          ? { ...t, replies: [...t.replies, r], parent: { ...t.parent, repliesCount: t.parent.repliesCount + 1 } }
          : t));
        this.total.update(v => v + 1); this.replyText = ''; this.replyingTo.set(null); this.submitting.set(false);
        this.toast.success('Reply posted!');
      },
      error: () => { this.toast.error('Failed to post reply.'); this.submitting.set(false); }
    });
  }

  likeComment(c: CommentDto): void {
    if (!this.auth.isLoggedIn()) { this.loginModal.open(); return; }
    this.likedSet.has(c.id) ? this.likedSet.delete(c.id) : this.likedSet.add(c.id);
    this.reactionSvc.toggleCommentLike(c.id).subscribe({ error: () => { this.likedSet.has(c.id) ? this.likedSet.delete(c.id) : this.likedSet.add(c.id); } });
  }

  isLiked(id: string): boolean { return this.likedSet.has(id); }

  deleteComment(id: string): void {
    if (!confirm('Delete this response?')) return;
    this.commentSvc.delete(id).subscribe({
      next: () => { this.threads.update(t => t.filter(x => x.parent.id !== id)); this.total.update(v => Math.max(0, v - 1)); this.toast.success('Deleted.'); },
      error: () => this.toast.error('Failed to delete.')
    });
  }

  deleteReply(parentId: string, replyId: string): void {
    if (!confirm('Delete this reply?')) return;
    this.commentSvc.delete(replyId).subscribe({
      next: () => {
        this.threads.update(ts => ts.map(t => t.parent.id === parentId
          ? { ...t, replies: t.replies.filter(r => r.id !== replyId), parent: { ...t.parent, repliesCount: Math.max(0, t.parent.repliesCount - 1) } }
          : t));
        this.total.update(v => Math.max(0, v - 1)); this.toast.success('Reply deleted.');
      },
      error: () => this.toast.error('Failed to delete.')
    });
  }

  canDelete(c: CommentDto): boolean { return this.auth.currentUser()?.id === c.author?.id; }
  cancelCompose(): void { this.composeFocused.set(false); this.newText = ''; }
}

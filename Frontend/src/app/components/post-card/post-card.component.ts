import { Component, Input, inject, signal, OnInit, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PostSummaryDto } from '../../models/post.models';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ToastService } from '../../services/ui.services';
import { ReactionService } from '../../services/blog.services';

@Component({
  selector: 'app-post-card',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './post-card.component.html',
  styleUrls: ['./post-card.component.css']
})
export class PostCardComponent implements OnInit {
  @Input({ required: true }) post!: PostSummaryDto;

  readonly auth        = inject(AuthService);
  readonly loginModal  = inject(LoginModalService);
  readonly reactionSvc = inject(ReactionService);
  readonly toast       = inject(ToastService);

  liked      = signal(false);
  likeCount  = signal(0);
  imgBroken  = signal(false);   // track load errors

  ngOnInit(): void { this.likeCount.set(0); this.imgBroken.set(false); }

  /** True only when coverImageUrl is a non-empty string and hasn't errored */
  get hasCover(): boolean {
    return !!(this.post.coverImageUrl?.trim()) && !this.imgBroken();
  }

  onImgError(): void { this.imgBroken.set(true); }

  toggleLike(e: MouseEvent): void {
    e.preventDefault(); e.stopPropagation();
    if (!this.auth.isLoggedIn()) { this.loginModal.open(); return; }
    const was = this.liked();
    this.liked.set(!was);
    this.likeCount.update(v => was ? Math.max(0, v - 1) : v + 1);
    this.reactionSvc.togglePostLike(this.post.id).subscribe({
      next: r => { this.liked.set(r.liked); this.likeCount.set(r.totalLikes); },
      error: () => {
        this.liked.set(was);
        this.likeCount.update(v => was ? v + 1 : Math.max(0, v - 1));
        this.toast.error('Could not update like');
      }
    });
  }

  share(e: MouseEvent): void {
    e.preventDefault(); e.stopPropagation();
    navigator.clipboard.writeText(`${window.location.origin}/blog/${this.post.slug}`)
      .then(() => this.toast.success('Link copied!'))
      .catch(() => this.toast.error('Could not copy link'));
  }
}

import { Component, Input, inject, signal, OnInit, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { PostSummaryDto } from '../../models/post.models';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ToastService } from '../../services/ui.services';
import { ReactionService } from '../../services/blog.services';
import { environment } from '../../../environments/environment';

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
  imgBroken  = signal(false);

  // Base URL for locally-uploaded media files
  readonly mediaBase = environment.apiUrl.replace('/api', '');

  ngOnInit(): void { this.likeCount.set(this.post.likesCount ?? 0); this.imgBroken.set(false); }

  get hasCover(): boolean {
    return !!(this.post.coverImageUrl?.trim()) && !this.imgBroken();
  }

  /** Resolve a media URL — absolute if it starts with http, otherwise prepend backend base */
  mediaUrl(url: string): string {
    return url.startsWith('http') ? url : `${this.mediaBase}${url}`;
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

import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, SlicePipe } from '@angular/common';
import { PostService } from '../../services/post.service';
import { ReactionService } from '../../services/blog.services';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ToastService } from '../../services/ui.services';
import { PostDetailDto, PostSummaryDto } from '../../models/post.models';
import { CommentComponent } from '../../components/comment/comment.component';
import { PostCardComponent } from '../../components/post-card/post-card.component';

@Component({
  selector: 'app-blog-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, SlicePipe, CommentComponent, PostCardComponent],
  templateUrl: './blog-detail.component.html',
  styleUrls: ['./blog-detail.component.css']
})
export class BlogDetailComponent implements OnInit {
  readonly route       = inject(ActivatedRoute);
  readonly router      = inject(Router);
  readonly postSvc     = inject(PostService);
  readonly reactionSvc = inject(ReactionService);
  readonly auth        = inject(AuthService);
  readonly loginModal  = inject(LoginModalService);
  readonly toast       = inject(ToastService);

  post         = signal<PostDetailDto | null>(null);
  relatedPosts = signal<PostSummaryDto[]>([]);
  loading      = signal(true);
  liked        = signal(false);
  likeCount    = signal(0);

  isAuthor = computed(() => {
    const me = this.auth.currentUser();
    return !!(me && this.post() && me.id === this.post()!.author.id);
  });

  readTime(): number {
    const words = (this.post()?.contentHtml || '').replace(/<[^>]*>/g, ' ').split(/\s+/).filter(Boolean).length;
    return Math.max(1, Math.ceil(words / 200));
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(p => {
      const slug = p.get('slug');
      if (slug) this.load(slug);
    });
  }

  load(slug: string): void {
    this.loading.set(true);
    this.relatedPosts.set([]);
    this.postSvc.getBySlug(slug).subscribe({
      next: p => {
        this.post.set(p);
        this.likeCount.set(p.likesCount ?? 0);
        this.loading.set(false);
        this.loadRelated(p);
      },
      error: () => this.loading.set(false)
    });
  }

  loadRelated(p: PostDetailDto): void {
    // Fetch posts matching first tag or category, exclude current
    const tag = p.tags[0] ?? undefined;
    const cat = p.categories[0] ?? undefined;
    this.postSvc.getPublished(1, 10, undefined, tag, cat).subscribe({
      next: r => {
        const filtered = r.items
          .filter(x => x.id !== p.id)
          .slice(0, 3);
        // If not enough, try without filters
        if (filtered.length < 3) {
          this.postSvc.getPublished(1, 10).subscribe({
            next: r2 => {
              const fallback = r2.items.filter(x => x.id !== p.id && !filtered.find(f => f.id === x.id));
              this.relatedPosts.set([...filtered, ...fallback].slice(0, 3));
            }
          });
        } else {
          this.relatedPosts.set(filtered);
        }
      }
    });
  }

  toggleLike(): void {
    if (!this.auth.isLoggedIn()) { this.loginModal.open(); return; }
    const was = this.liked();
    this.liked.set(!was);
    this.likeCount.update(v => was ? Math.max(0, v - 1) : v + 1);
    this.reactionSvc.togglePostLike(this.post()!.id).subscribe({
      next: r  => { this.liked.set(r.liked); this.likeCount.set(r.totalLikes); },
      error: () => { this.liked.set(was); this.likeCount.update(v => was ? v + 1 : Math.max(0, v - 1)); }
    });
  }

  share(): void {
    navigator.clipboard.writeText(window.location.href)
      .then(()  => this.toast.success('Link copied!'))
      .catch(() => this.toast.info(window.location.href));
  }

  deletePost(): void {
    if (!confirm('Permanently delete this story?')) return;
    this.postSvc.delete(this.post()!.id).subscribe({
      next: () => { this.toast.success('Story deleted.'); this.router.navigate(['/']); },
      error: () => this.toast.error('Failed to delete.')
    });
  }
}

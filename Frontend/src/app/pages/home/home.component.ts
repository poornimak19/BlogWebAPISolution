import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PostService } from '../../services/post.service';
import { TaxonomyService } from '../../services/blog.services';
import { AuthService } from '../../services/auth.service';
import { LoginModalService, ToastService } from '../../services/ui.services';
import { PostCardComponent } from '../../components/post-card/post-card.component';
import { PostSummaryDto } from '../../models/post.models';
import { TagDto, CategoryDto } from '../../models/blog.models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [PostCardComponent, RouterLink],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  readonly postSvc     = inject(PostService);
  readonly taxonomySvc = inject(TaxonomyService);
  readonly auth        = inject(AuthService);
  readonly loginModal  = inject(LoginModalService);
  readonly toast       = inject(ToastService);

  posts      = signal<PostSummaryDto[]>([]);
  tags       = signal<TagDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  total      = signal(0);
  page       = signal(1);
  loading    = signal(false);

  // Multi-select: store arrays of selected slugs
  selectedTags = signal<string[]>([]);
  selectedCats = signal<string[]>([]);
  sort         = signal<'latest' | 'popular'>('latest');

  readonly pageSize  = 6;
  readonly skeletons = Array(9).fill(0);

  latestCover = computed(() => {
  const list = this.posts();
  return list.length > 0 ? list[0].coverImageUrl : null;
});


  totalPages  = computed(() => Math.ceil(this.total() / this.pageSize));
  pageNumbers = computed(() => {
    const t = this.totalPages(), c = this.page(), pages: number[] = [];
    const start = Math.max(1, c - 2), end = Math.min(t, c + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  });

  ngOnInit(): void {
    this.loadPosts();
    this.taxonomySvc.getTags().subscribe({ next: t => this.tags.set(t) });
    this.taxonomySvc.getCategories().subscribe({ next: c => this.categories.set(c) });
  }

  loadPosts(): void {
    this.loading.set(true);
    const tag = this.selectedTags().length > 0 ? this.selectedTags()[0] : undefined;
    const cat = this.selectedCats().length > 0 ? this.selectedCats()[0] : undefined;
    this.postSvc.getPublished(this.page(), this.pageSize, undefined, tag, cat).subscribe({
      next: r => {
        let items = r.items;
        if (this.selectedTags().length > 1)
          items = items.filter(p => this.selectedTags().every(s => p.tags.includes(s)));
        if (this.selectedCats().length > 1)
          items = items.filter(p => this.selectedCats().every(s => p.categories.includes(s)));
        if (this.sort() === 'popular')
          items = [...items].sort((a, b) => (b.likesCount ?? 0) - (a.likesCount ?? 0)); //null coleascing
        this.posts.set(items);
        this.total.set(r.total);
        this.loading.set(false);
      },
      error: () => { this.toast.error('Failed to load stories.'); this.loading.set(false); }
    });
  }

  toggleTag(slug: string): void {
    this.selectedTags.update(ts => ts.includes(slug) ? ts.filter(t => t !== slug) : [...ts, slug]);
    this.page.set(1);
    this.loadPosts();
  }

  toggleCat(slug: string): void {
    this.selectedCats.update(cs => cs.includes(slug) ? cs.filter(c => c !== slug) : [...cs, slug]);
    this.page.set(1);
    this.loadPosts();
  }

  clearAllFilters(): void {
    this.selectedTags.set([]);
    this.selectedCats.set([]);
    this.page.set(1);
    this.loadPosts();
  }

  hasFilters = computed(() => this.selectedTags().length > 0 || this.selectedCats().length > 0);

  setSort(s: 'latest' | 'popular'): void { this.sort.set(s); this.page.set(1); this.loadPosts(); }
  goTo(p: number): void {
  if (p < 1 || p > this.totalPages()) return;
  this.page.set(p);
  this.loadPosts();
  // no scroll — stays in place
}
}

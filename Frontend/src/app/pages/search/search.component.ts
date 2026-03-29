
import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PostService } from '../../services/post.service';
import { TaxonomyService } from '../../services/blog.services';
import { ToastService } from '../../services/ui.services';
import { PostSummaryDto } from '../../models/post.models';
import { TagDto, CategoryDto } from '../../models/blog.models';
import { PostCardComponent } from '../../components/post-card/post-card.component';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [FormsModule, PostCardComponent],
  templateUrl: './search.component.html',
  styleUrls: ['./search.component.css']
})
export class SearchComponent implements OnInit {
  readonly route       = inject(ActivatedRoute);
  readonly router      = inject(Router);
  readonly postSvc     = inject(PostService);
  readonly taxonomySvc = inject(TaxonomyService);
  readonly toast       = inject(ToastService);

  posts      = signal<PostSummaryDto[]>([]);
  tags       = signal<TagDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  total      = signal(0);
  page       = signal(1);
  loading    = signal(false);
  hasSearched = signal(false);

  // Multi-select
  selectedTags = signal<string[]>([]);
  selectedCats = signal<string[]>([]);

  query = '';
  sort  = 'recent';
  readonly pageSize  = 6;
  readonly skeletons = Array(9).fill(0);

  totalPages = computed(() => Math.ceil(this.total() / this.pageSize));
  pageNumbers = computed(() => {
    const t = this.totalPages(), c = this.page(), pages: number[] = [];
    const start = Math.max(1, c - 2), end = Math.min(t, c + 2);
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  });

  hasFilters = computed(() =>
    this.selectedTags().length > 0 ||
    this.selectedCats().length > 0 ||
    !!this.query
  );

  ngOnInit(): void {
    this.taxonomySvc.getTags().subscribe({ next: t => this.tags.set(t) });
    this.taxonomySvc.getCategories().subscribe({ next: c => this.categories.set(c) });

    this.route.queryParams.subscribe(params => {
      if (params['q'])        this.query = params['q'];
      if (params['tag'])      this.selectedTags.set([params['tag']]);
      if (params['category']) this.selectedCats.set([params['category']]);
      this.doSearch();
    });
  }

  doSearch(): void {
    this.page.set(1);
    this.fetch(1);
  }

  private fetch(p: number): void {
    this.hasSearched.set(true);
    this.loading.set(true);

    const tag = this.selectedTags().length > 0 ? this.selectedTags()[0] : undefined;
    const cat = this.selectedCats().length > 0 ? this.selectedCats()[0] : undefined;
    const q   = this.query.trim() || undefined;

    this.postSvc.getPublished(p, this.pageSize, q, tag, cat).subscribe({
      next: r => {
        let list = r.items;

        if (this.selectedTags().length > 1)
          list = list.filter(post => this.selectedTags().every(s => post.tags.includes(s)));
        if (this.selectedCats().length > 1)
          list = list.filter(post => this.selectedCats().every(s => post.categories.includes(s)));
        if (this.sort === 'popular')
          list = [...list].sort((a, b) => (b.likesCount ?? 0) - (a.likesCount ?? 0));

        this.posts.set(list);
        this.total.set(r.total);
        this.loading.set(false);
      },
      error: () => {
        this.toast.error("Search failed.");
        this.loading.set(false);
      }
    });
  }

  toggleTag(slug: string): void {
    this.selectedTags.update(ts =>
      ts.includes(slug) ? ts.filter(t => t !== slug) : [...ts, slug]
    );
    this.doSearch();
  }

  toggleCat(slug: string): void {
    this.selectedCats.update(cs =>
      cs.includes(slug) ? cs.filter(c => c !== slug) : [...cs, slug]
    );
    this.doSearch();
  }

  removeTag(s: string): void {
    this.selectedTags.update(ts => ts.filter(t => t !== s));
    this.doSearch();
  }

  removeCat(s: string): void {
    this.selectedCats.update(cs => cs.filter(c => c !== s));
    this.doSearch();
  }

  goTo(p: number): void {
    this.page.set(p);
    this.fetch(p);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  reset(): void {
    this.query = '';
    this.selectedTags.set([]);
    this.selectedCats.set([]);
    this.sort = 'recent';
    this.page.set(1);
    this.hasSearched.set(false);
    this.fetch(1);
  }

  setSort(s: string): void {
    this.sort = s;
    this.page.set(1);
    this.fetch(1);
  }

}

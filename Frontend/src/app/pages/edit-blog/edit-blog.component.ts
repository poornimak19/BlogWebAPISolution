import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PostService } from '../../services/post.service';
import { TaxonomyService } from '../../services/blog.services';
import { ToastService } from '../../services/ui.services';
import { PostDetailDto } from '../../models/post.models';
import { CategoryDto, TagDto } from '../../models/blog.models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-edit-blog',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './edit-blog.component.html',
  styleUrls: ['./edit-blog.component.css']
})
export class EditBlogComponent implements OnInit {
  readonly route       = inject(ActivatedRoute);
  readonly router      = inject(Router);
  readonly postSvc     = inject(PostService);
  readonly taxonomySvc = inject(TaxonomyService);  
  readonly toast       = inject(ToastService);

  post       = signal<PostDetailDto | null>(null);
  loading    = signal(true);
  saving     = signal(false);
  categories = signal<CategoryDto[]>([]);
  tags       = signal<TagDto[]>([]);
  html       = signal('');
  tagInput      = '';
  categoryInput = '';

  audioUploading = signal(false);
  videoUploading = signal(false);
  audioUrl       = signal<string | null>(null);
  videoUrl       = signal<string | null>(null);
  audioFileName  = signal<string | null>(null);
  videoFileName  = signal<string | null>(null);

  readonly mediaBase = environment.apiUrl.replace('/api', '');

  form = {
    title: '', excerpt: '', slug: '', visibility: 'Public' as string,
    coverImageUrl: '', tagNames: [] as string[], categoryNames: [] as string[],
    commentsEnabled: true, autoApprove: true, isPremium: false
  };

  wordCount = () => this.html().replace(/<[^>]*>/g, ' ').trim().split(/\s+/).filter(Boolean).length;

  suggestedTags        = () => this.tags().filter(t => !this.form.tagNames.includes(t.name.toLowerCase())).slice(0, 6);
  suggestedCategories  = () => this.categories().filter(c => !this.form.categoryNames.includes(c.name)).slice(0, 6);

  mediaUrl(url: string): string {
    return url.startsWith('http') ? url : `${this.mediaBase}${url}`;
  }

  ngOnInit(): void {
    this.taxonomySvc.getTags().subscribe({ next: t => this.tags.set(t) });
    this.taxonomySvc.getCategories().subscribe({ next: c => this.categories.set(c) });
    this.route.paramMap.subscribe(p => { const id = p.get('id'); if (id) this.loadPost(id); });
  }

  loadPost(id: string): void {
    this.loading.set(true);
    this.postSvc.getById(id).subscribe({
      next: p => {
        this.post.set(p);
        this.form.title           = p.title;
        this.form.excerpt         = p.excerpt || '';
        this.form.slug            = p.slug;
        this.form.visibility      = p.visibility;
        this.form.coverImageUrl   = p.coverImageUrl || '';
        this.form.tagNames        = [...p.tags];
        this.form.categoryNames   = [...p.categories];
        this.form.commentsEnabled = p.commentsEnabled;
        this.form.autoApprove     = p.autoApproveComments;
        this.form.isPremium       = p.isPremium;
        this.html.set(p.contentHtml);

        // Pre-populate existing media
        if (p.audioUrl) { this.audioUrl.set(p.audioUrl); this.audioFileName.set('Existing audio'); }
        if (p.videoUrl) { this.videoUrl.set(p.videoUrl); this.videoFileName.set('Existing video'); }

        this.loading.set(false);
        setTimeout(() => { const el = document.getElementById('rte-edit'); if (el && !el.innerHTML) el.innerHTML = p.contentHtml; }, 80);
      },
      error: () => this.loading.set(false)
    });
  }

  autoResize(e: Event): void { const el = e.target as HTMLTextAreaElement; el.style.height = 'auto'; el.style.height = el.scrollHeight + 'px'; }
  onInput(e: Event): void { this.html.set((e.target as HTMLElement).innerHTML); }
  onPaste(e: ClipboardEvent): void { e.preventDefault(); document.execCommand('insertText', false, e.clipboardData?.getData('text/plain') ?? ''); }

  cmd(command: string, value?: string): void {
    document.execCommand(command, false, value);
    const el = document.getElementById('rte-edit');
    if (el) { this.html.set(el.innerHTML); el.focus(); }
  }

  insertLink():  void { const u = prompt('URL:');       if (u?.trim()) this.cmd('createLink',  u.trim()); }
  insertImage(): void { const u = prompt('Image URL:'); if (u?.trim()) this.cmd('insertImage', u.trim()); }

  addTag(e: Event): void {
    e.preventDefault();
    const v = this.tagInput.trim().toLowerCase();
    if (v && !this.form.tagNames.includes(v)) this.form.tagNames = [...this.form.tagNames, v];
    this.tagInput = '';
  }
  addSuggestedTag(name: string): void {
    const v = name.toLowerCase();
    if (!this.form.tagNames.includes(v)) this.form.tagNames = [...this.form.tagNames, v];
  }
  removeTag(tag: string): void { this.form.tagNames = this.form.tagNames.filter(t => t !== tag); }

  addCategory(e: Event): void {
    e.preventDefault();
    const v = this.categoryInput.trim();
    if (v && !this.form.categoryNames.includes(v)) this.form.categoryNames = [...this.form.categoryNames, v];
    this.categoryInput = '';
  }
  addSuggestedCategory(name: string): void {
    if (!this.form.categoryNames.includes(name)) this.form.categoryNames = [...this.form.categoryNames, name];
  }
  removeCategory(name: string): void { this.form.categoryNames = this.form.categoryNames.filter(c => c !== name); }

  onAudioSelected(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.audioUploading.set(true);
    this.audioFileName.set(file.name);
    this.postSvc.uploadMedia(file).subscribe({
      next: r => { this.audioUrl.set(r.url); this.audioUploading.set(false); },
      error: () => { this.toast.error('Audio upload failed.'); this.audioUploading.set(false); this.audioFileName.set(null); }
    });
  }

  onVideoSelected(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.videoUploading.set(true);
    this.videoFileName.set(file.name);
    this.postSvc.uploadMedia(file).subscribe({
      next: r => { this.videoUrl.set(r.url); this.videoUploading.set(false); },
      error: () => { this.toast.error('Video upload failed.'); this.videoUploading.set(false); this.videoFileName.set(null); }
    });
  }

  removeAudio(): void { this.audioUrl.set(null); this.audioFileName.set(null); }
  removeVideo(): void { this.videoUrl.set(null); this.videoFileName.set(null); }

  save(status: string): void {
    if (!this.form.title.trim()) { this.toast.error('Title is required.'); return; }
    this.saving.set(true);
    this.postSvc.update(this.post()!.id, {
      title: this.form.title.trim(),
      slug: this.form.slug.trim() || undefined,
      excerpt: this.form.excerpt.trim() || undefined,
      contentHtml: this.html(),
      visibility: this.form.visibility,
      tagNames: this.form.tagNames,
      categoryNames: this.form.categoryNames,
      commentsEnabled: this.form.commentsEnabled,
      autoApproveComments: this.form.autoApprove,
      coverImageUrl: this.form.coverImageUrl,
      audioUrl: this.audioUrl(),
      videoUrl: this.videoUrl(),
      isPremium: this.form.isPremium,
      status
    }).subscribe({
      next: p => { this.toast.success('Story updated!'); this.saving.set(false); this.router.navigate(['/blog', p.slug]); },
      error: e => { this.toast.error(e.error?.message || 'Update failed.'); this.saving.set(false); }
    });
  }

  deletePost(): void {
    if (!confirm('Permanently delete this story? This cannot be undone.')) return;
    this.postSvc.delete(this.post()!.id).subscribe({
      next: () => { this.toast.success('Story deleted.'); this.router.navigate(['/me']); },
      error: () => this.toast.error('Failed to delete.')
    });
  }
}

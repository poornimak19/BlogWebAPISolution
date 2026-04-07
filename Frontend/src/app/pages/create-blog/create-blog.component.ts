import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PostService } from '../../services/post.service';
import { TaxonomyService,UserService } from '../../services/blog.services';
import { ToastService } from '../../services/ui.services';
import { TagDto, CategoryDto,UserSearchDto } from '../../models/blog.models';

@Component({
  selector: 'app-create-blog',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './create-blog.component.html',
  styleUrls: ['./create-blog.component.css']
})
export class CreateBlogComponent implements OnInit {
  readonly router      = inject(Router);
  readonly postSvc     = inject(PostService);
  readonly taxonomySvc = inject(TaxonomyService);
  readonly userSvc     = inject(UserService);
  readonly toast       = inject(ToastService);

  saving     = signal(false);
  tags       = signal<TagDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  html       = signal('');
  tagInput   = '';
  categoryInput = '';
  userSearchQuery   = '';
  userSearchResults = signal<UserSearchDto[]>([]);
  allowedUsers      = signal<UserSearchDto[]>([]);
  searching         = signal(false);

  audioUploading = signal(false);
  videoUploading = signal(false);
  audioUrl       = signal<string | null>(null);
  videoUrl       = signal<string | null>(null);
  audioFileName  = signal<string | null>(null);
  videoFileName  = signal<string | null>(null);

  form = {
    title: '', excerpt: '', slug: '',
    visibility: 'Public' as 'Public' | 'Private',
    coverImageUrl: '', tagNames: [] as string[], categoryNames: [] as string[],
    commentsEnabled: true, autoApprove: true, isPremium: false
  };

suggestedTags = () => this.tags().filter(t => !this.form.tagNames.includes(t.name.toLowerCase())).slice(0, 8);

  
suggestedCategories = () =>this.categories().filter(c => !this.form.categoryNames.includes(c.name)).slice(0, 8);

// NEW: add on Enter
addCategory(e: Event): void {
  e.preventDefault();
  const v = this.categoryInput.trim();
  if (v && !this.form.categoryNames.includes(v)) {
    this.form.categoryNames = [...this.form.categoryNames, v];
  }
  this.categoryInput = '';
}

// NEW: add from suggestion
addSuggestedCategory(name: string): void {
  const v = name.trim();
  if (!this.form.categoryNames.includes(v)) {
    this.form.categoryNames = [...this.form.categoryNames, v];
  }
}

// NEW: remove a selected category
removeCategory(name: string): void {
  this.form.categoryNames = this.form.categoryNames.filter(c => c !== name);
}


  
  wordCount     = () => this.html().replace(/<[^>]*>/g, ' ').trim().split(/\s+/).filter(Boolean).length;

  ngOnInit(): void {
    this.taxonomySvc.getTags().subscribe({ next: t => this.tags.set(t) });
    this.taxonomySvc.getCategories().subscribe({ next: c => this.categories.set(c) });
  }

    
   // ── Editor helpers
  autoResize(e: Event): void {
    const el = e.target as HTMLTextAreaElement;
    el.style.height = 'auto'; el.style.height = el.scrollHeight + 'px';
  }

  onInput(e: Event): void { this.html.set((e.target as HTMLElement).innerHTML); }

  onPaste(e: ClipboardEvent): void {
    e.preventDefault();
    document.execCommand('insertText', false, e.clipboardData?.getData('text/plain') ?? '');
  }

  cmd(command: string, value?: string): void {
    document.execCommand(command, false, value);
    const el = document.getElementById('rte');
    if (el) { this.html.set(el.innerHTML); el.focus(); }
  }

  insertLink():  void { const u = prompt('Enter URL:');       if (u?.trim()) this.cmd('createLink',  u.trim()); }
  insertImage(): void { const u = prompt('Enter image URL:'); if (u?.trim()) this.cmd('insertImage', u.trim()); }

  addTag(e: Event): void {
    e.preventDefault();
    const v = this.tagInput.trim().toLowerCase();
    if (v && !this.form.tagNames.includes(v)) this.form.tagNames = [...this.form.tagNames, v];
    this.tagInput = '';
  }

  addSuggested(name: string): void {
    const v = name.toLowerCase();
    if (!this.form.tagNames.includes(v)) this.form.tagNames = [...this.form.tagNames, v];
  }

  removeTag(tag: string): void { this.form.tagNames = this.form.tagNames.filter(t => t !== tag); }

  toggleCat(name: string): void {
    this.form.categoryNames = this.form.categoryNames.includes(name)
      ? this.form.categoryNames.filter(c => c !== name)
      : [...this.form.categoryNames, name];
  }

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

  save(status: 'Draft' | 'Published'): void {
    console.log(this.form.coverImageUrl)
    if (!this.form.title.trim()) { this.toast.error('Title is required.'); return; }
    const content = this.html();
    if (!content.replace(/<[^>]*>/g, '').trim()) { this.toast.error('Content cannot be empty.'); return; }
    
    this.saving.set(true);
    this.postSvc.create({
      title: this.form.title.trim(),
      slug: this.form.slug.trim() || undefined,
      excerpt: this.form.excerpt.trim() || undefined,
      contentHtml: content,
      visibility: this.form.visibility,
      tagNames: this.form.tagNames,
      categoryNames: this.form.categoryNames,
      allowedUserIds:     this.allowedUsers().map(u => u.id),
      commentsEnabled: this.form.commentsEnabled,
      autoApproveComments: this.form.autoApprove,
      coverImageUrl: this.form.coverImageUrl?.trim() ?? "",
      audioUrl: this.audioUrl() ?? undefined,
      videoUrl: this.videoUrl() ?? undefined,
      isPremium: this.form.isPremium
    }).subscribe({
      next: post => {
        if (status === 'Published') {
          this.postSvc.publish(post.id).subscribe({
            next: () => { this.toast.success('Story sent to admin review 🔗'); this.router.navigate(['/blog', post.slug]); this.saving.set(false); },
            error: () => { this.toast.info('Draft saved. Publish manually from My Posts.'); this.router.navigate(['/me']); this.saving.set(false); }
          });
        } else {
          this.toast.success('Draft saved!'); this.router.navigate(['/me']); this.saving.set(false);
        }
      },
      error: e => { this.toast.error(e.error?.message || 'Failed to save.'); this.saving.set(false); }
    });
  }
}

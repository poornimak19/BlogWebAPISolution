import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { TagDto, CategoryDto } from '../../../models/blog.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';

@Component({
  selector: 'app-admin-taxonomy',
  standalone: true,
  imports: [FormsModule, AdminNavComponent],
  templateUrl: './taxonomy.component.html',
  styleUrls: ['./taxonomy.component.css']
})
export class AdminTaxonomyComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);

  tags       = signal<TagDto[]>([]);
  categories = signal<CategoryDto[]>([]);
  loading    = signal(true);

  // New item inputs
  newTagName  = '';
  newCatName  = '';

  // Inline error messages for duplicate detection
  tagError = signal<string | null>(null);
  catError = signal<string | null>(null);

  // Inline rename state: { id, name }
  renamingTag = signal<{ id: number; name: string } | null>(null);
  renamingCat = signal<{ id: number; name: string } | null>(null);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.adminSvc.getTags().subscribe({ next: t => this.tags.set(t) });
    this.adminSvc.getCategories().subscribe({
      next: c => { this.categories.set(c); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  // ── Tags ─────────────────────────────────────────────────
  createTag(): void {
    const name = this.newTagName.trim();
    if (!name) return;
    this.tagError.set(null);
    this.adminSvc.createTag(name).subscribe({
      next: t => { this.tags.update(list => [...list, t]); this.newTagName = ''; this.toast.success('Tag created.'); },
      error: e => {
        const msg = e.error?.message || 'Failed to create tag.';
        this.tagError.set(msg);
      }
    });
  }

  startRenameTag(tag: TagDto): void { this.renamingTag.set({ id: tag.id, name: tag.name }); this.tagError.set(null); }
  cancelRenameTag(): void { this.renamingTag.set(null); }

  saveRenameTag(): void {
    const r = this.renamingTag();
    if (!r || !r.name.trim()) return;
    this.adminSvc.renameTag(r.id, r.name.trim()).subscribe({
      next: updated => { this.tags.update(list => list.map(t => t.id === r.id ? updated : t)); this.renamingTag.set(null); this.toast.success('Tag renamed.'); },
      error: e => this.toast.error(e.error?.message || 'Failed to rename tag.')
    });
  }

  deleteTag(tag: TagDto): void {
    if (!confirm(`Delete tag "${tag.name}"? This removes it from all posts.`)) return;
    this.adminSvc.deleteTag(tag.id).subscribe({
      next: () => { this.tags.update(list => list.filter(t => t.id !== tag.id)); this.toast.success('Tag deleted.'); },
      error: () => this.toast.error('Failed to delete tag.')
    });
  }

  // ── Categories ───────────────────────────────────────────
  createCategory(): void {
    const name = this.newCatName.trim();
    if (!name) return;
    this.catError.set(null);
    this.adminSvc.createCategory(name).subscribe({
      next: c => { this.categories.update(list => [...list, c]); this.newCatName = ''; this.toast.success('Category created.'); },
      error: e => {
        const msg = e.error?.message || 'Failed to create category.';
        this.catError.set(msg);
      }
    });
  }

  startRenameCat(cat: CategoryDto): void { this.renamingCat.set({ id: cat.id, name: cat.name }); this.catError.set(null); }
  cancelRenameCat(): void { this.renamingCat.set(null); }

  saveRenameCat(): void {
    const r = this.renamingCat();
    if (!r || !r.name.trim()) return;
    this.adminSvc.renameCategory(r.id, r.name.trim()).subscribe({
      next: updated => { this.categories.update(list => list.map(c => c.id === r.id ? updated : c)); this.renamingCat.set(null); this.toast.success('Category renamed.'); },
      error: e => this.toast.error(e.error?.message || 'Failed to rename category.')
    });
  }

  deleteCategory(cat: CategoryDto): void {
    if (!confirm(`Delete category "${cat.name}"? This removes it from all posts.`)) return;
    this.adminSvc.deleteCategory(cat.id).subscribe({
      next: () => { this.categories.update(list => list.filter(c => c.id !== cat.id)); this.toast.success('Category deleted.'); },
      error: () => this.toast.error('Failed to delete category.')
    });
  }
}

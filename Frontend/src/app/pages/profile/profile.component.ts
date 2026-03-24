import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { UserService, FollowService } from '../../services/blog.services';
import { PostService } from '../../services/post.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/ui.services';
import { UserProfileDto } from '../../models/blog.models';
import { PostSummaryDto } from '../../models/post.models';
import { PostCardComponent } from '../../components/post-card/post-card.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, PostCardComponent],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  readonly route     = inject(ActivatedRoute);
  readonly userSvc   = inject(UserService);
  readonly followSvc = inject(FollowService);
  readonly postSvc   = inject(PostService);
  readonly auth      = inject(AuthService);
  readonly toast     = inject(ToastService);

  profile       = signal<UserProfileDto | null>(null);
  posts         = signal<PostSummaryDto[]>([]);
  drafts        = signal<PostSummaryDto[]>([]);
  loading       = signal(true);
  saving        = signal(false);
  isFollowing   = signal(false);
  editMode      = signal(false);
  activeTab     = signal<'posts' | 'drafts' | 'following'>('posts');
  avatarPreview = signal<string>('');   // base64 preview from file picker

  ef = { displayName: '', bio: '', avatarUrl: '' };

  isOwnProfile = computed(() => {
    const me = this.auth.currentUser();
    return !!(me && this.profile() && me.username === this.profile()!.username);
  });
  isReader  = computed(() => this.auth.userRole() === 'Reader');
  isBlogger = computed(() => this.auth.userRole() === 'Blogger' || this.auth.userRole() === 'Admin');

  avatarLetter = computed(() => {
    const p = this.profile();
    return (p?.displayName || p?.username || '?')[0].toUpperCase();
  });

  avatarSrc = computed(() => {
    // Priority: local preview > saved URL > null (show letter)
    return this.avatarPreview() || this.profile()?.avatarUrl || '';
  });

  publishedCount = computed(() => this.posts().length);

  ngOnInit(): void {
    const username = this.route.snapshot.paramMap.get('username');
    if (username) this.loadByUsername(username);
    else this.loadOwn();
  }

  loadByUsername(username: string): void {
    this.loading.set(true);
    this.userSvc.getByUsername(username).subscribe({
      next: p => { this.profile.set(p); this.loading.set(false); this.loadPublished(username); },
      error: () => this.loading.set(false)
    });
  }

  loadOwn(): void {
    this.loading.set(true);
    this.userSvc.getMyProfile().subscribe({
      next: p => {
        this.profile.set(p);
        this.ef = { displayName: p.displayName || '', bio: p.bio || '', avatarUrl: p.avatarUrl || '' };
        this.loading.set(false);
        if (this.isReader()) this.activeTab.set('following');
        else this.loadAllMyPosts();
      },
      error: () => this.loading.set(false)
    });
  }

  loadPublished(username: string): void {
    this.postSvc.getPublished(1, 50).subscribe({
      next: r => this.posts.set(r.items.filter(p => p.author.username === username))
    });
  }

  loadAllMyPosts(): void {
    this.postSvc.getMyPosts(1, 100).subscribe({
      next: r => {
        this.posts.set(r.items.filter(p => p.status === 'Published'));
        this.drafts.set(r.items.filter(p => p.status === 'Draft'));
      }
    });
  }

  switchToDrafts(): void { this.activeTab.set('drafts'); if (this.isOwnProfile() && this.drafts().length === 0) this.loadAllMyPosts(); }
  switchToFollowing(): void { this.activeTab.set('following'); }

  toggleFollow(): void {
    const p = this.profile(); if (!p) return;
    this.followSvc.toggle(p.id).subscribe({
      next: r => { this.isFollowing.set(r.following); this.profile.update(old => old ? { ...old, followers: r.followersCount } : old); this.toast.success(r.following ? `Following ${p.username}` : `Unfollowed ${p.username}`); },
      error: e => this.toast.error(e.error?.message || 'Action failed.')
    });
  }

  openEdit(): void {
    const p = this.profile();
    if (p) this.ef = { displayName: p.displayName || '', bio: p.bio || '', avatarUrl: p.avatarUrl || '' };
    this.avatarPreview.set('');
    this.editMode.set(true);
  }

  closeOverlay(e: MouseEvent): void {
    if ((e.target as HTMLElement).classList.contains('edit-overlay')) this.editMode.set(false);
  }

  /** Called when user picks a file from file manager */
  onAvatarFileChange(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) { this.toast.error('Please select an image file.'); return; }
    if (file.size > 5 * 1024 * 1024) { this.toast.error('Image must be under 5 MB.'); return; }
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      this.avatarPreview.set(dataUrl);
      this.ef.avatarUrl = dataUrl;   // store base64 in avatarUrl field for save
    };
    reader.readAsDataURL(file);
  }

  clearAvatar(): void { this.avatarPreview.set(''); this.ef.avatarUrl = ''; }

  saveProfile(): void {
    this.saving.set(true);
    this.userSvc.updateProfile({
      displayName: this.ef.displayName || undefined,
      bio:         this.ef.bio || undefined,
      avatarUrl:   this.ef.avatarUrl || undefined
    }).subscribe({
      next: p => {
        this.profile.set(p);
        this.avatarPreview.set('');
        this.editMode.set(false);
        this.saving.set(false);
        this.toast.success('Profile updated!');
      },
      error: () => { this.toast.error('Update failed.'); this.saving.set(false); }
    });
  }

  deleteDraft(post: PostSummaryDto): void {
    if (!confirm(`Delete draft "${post.title}"?`)) return;
    this.postSvc.delete(post.id).subscribe({
      next: () => { this.drafts.update(d => d.filter(p => p.id !== post.id)); this.toast.success('Draft deleted.'); },
      error: () => this.toast.error('Failed to delete.')
    });
  }
}

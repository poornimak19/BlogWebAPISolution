import { Routes } from '@angular/router';
import { authGuard, bloggerGuard, adminGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'blog/:slug',
    loadComponent: () => import('./pages/blog-detail/blog-detail.component').then(m => m.BlogDetailComponent)
  },
  {
    path: 'create',
    loadComponent: () => import('./pages/create-blog/create-blog.component').then(m => m.CreateBlogComponent),
    canActivate: [authGuard, bloggerGuard]
  },
  {
    path: 'edit/:id',
    loadComponent: () => import('./pages/edit-blog/edit-blog.component').then(m => m.EditBlogComponent),
    canActivate: [authGuard, bloggerGuard]
  },
  {
    path: 'profile/:username',
    loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent)
  },
  {
    path: 'me',
    loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent),
    canActivate: [authGuard]
  },
  {
    path: 'search',
    loadComponent: () => import('./pages/search/search.component').then(m => m.SearchComponent)
  },
  {
    path: 'premium',
    loadComponent: () => import('./pages/premium/premium.component').then(m => m.PremiumComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./pages/reset-password/reset-password.component').then(m => m.ResetPasswordComponent)
  },
  // ── Admin routes (Admin only) ─────────────────────────────
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/dashboard/dashboard.component').then(m => m.AdminDashboardComponent)
  },
  {
    path: 'admin/users',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/users/users.component').then(m => m.AdminUsersComponent)
  },
  {
    path: 'admin/posts',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/posts/posts.component').then(m => m.AdminPostsComponent)
  },
  {
    path: 'admin/comments',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/comments/comments.component').then(m => m.AdminCommentsComponent)
  },
  {
    path: 'admin/blogs',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/blogs/blogs.component').then(m => m.AdminBlogsComponent)
  },
  {
    path: 'admin/audit-logs',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/audit-logs/audit-logs.component').then(m => m.AuditLogsComponent)
  },
  {
    path: 'admin/taxonomy',
    canActivate: [authGuard, adminGuard],
    loadComponent: () => import('./pages/admin/taxonomy/taxonomy.component').then(m => m.AdminTaxonomyComponent)
  },
  {
    path: '404',
    loadComponent: () => import('./pages/not-found/not-found.component').then(m => m.NotFoundComponent)
  },
  {
    path: '**',
    loadComponent: () => import('./pages/not-found/not-found.component').then(m => m.NotFoundComponent)
  }
];

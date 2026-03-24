# Inkwell — Angular 21 Blog Frontend

Complete production-ready Angular **21** frontend for the BlogWebAPI (.NET) backend.

---

## 🚀 Quick Start

```bash
# Prerequisites: Node 20+, Angular CLI 21
npm install -g @angular/cli

# Install
npm install

# Run (proxy included)
npm start
# → http://localhost:4200

# Tests
npm test

# Production build
npm run build:prod
```

> Backend must be running at `http://localhost:5034`

---

## 📁 Structure — Every component has 4 separate files

```
src/app/
├── app.component.ts / .html / .css / .spec.ts
├── app.config.ts          ← Angular 21 providers
├── app.routes.ts          ← Dynamic loadComponent routes
│
├── components/            ← 7 shared components × 4 files each
│   ├── spinner/           ← Global loading overlay
│   ├── toast/             ← Auto-dismiss notifications
│   ├── navbar/            ← Auth-aware sticky nav
│   ├── footer/            ← Simple footer
│   ├── login-modal/       ← Login / Register / Forgot popup
│   ├── post-card/         ← Reusable card with optimistic like
│   └── comment/           ← Threaded comments with replies
│
├── pages/                 ← 8 pages × 4 files each
│   ├── home/              ← Feed, tag filters, pagination, skeleton
│   ├── blog-detail/       ← Full post, skeleton, comments, like
│   ├── create-blog/       ← Rich-text editor, tags, categories
│   ├── edit-blog/         ← Same editor, status badge, delete
│   ├── profile/           ← Follow, edit, posts, drafts
│   ├── search/            ← Filters, skeleton, pagination
│   ├── reset-password/    ← Token-based reset
│   └── not-found/         ← 404 page
│
├── services/
│   ├── auth.service.ts    ← Register, Login, Me, ForgotPwd, Reset
│   ├── post.service.ts    ← CRUD, getPublished, mine, publish
│   ├── blog.services.ts   ← Comment, Reaction, Follow, Taxonomy, User
│   └── ui.services.ts     ← LoadingService, ToastService, LoginModalService
│
├── models/
│   ├── auth.models.ts     ← Matches backend AuthDTOs exactly
│   ├── post.models.ts     ← Matches backend PostDTOs exactly
│   └── blog.models.ts     ← Matches backend all other DTOs exactly
│
├── guards/
│   └── auth.guard.ts      ← authGuard, bloggerGuard, adminGuard
│
└── interceptors/
    └── jwt.interceptor.ts ← Bearer token + 401/403/500 handling

src/environments/
├── environment.ts         ← Dev: http://localhost:5034/api
└── environment.prod.ts    ← Prod: update with your domain
```

---

## 🔗 All Backend APIs Connected

| Feature | Endpoint |
|---|---|
| Register / Login / Me | `POST /api/auth/register` · `POST /api/auth/login` · `GET /api/auth/me` |
| Forgot / Reset password | `POST /api/auth/forgot-password` · `POST /api/auth/reset-password` |
| Published posts | `GET /api/posts/published?page&pageSize&q&tag&category` |
| Post by slug | `GET /api/posts/slug/{slug}` |
| My posts | `GET /api/posts/mine` |
| Create / Update / Delete | `POST /api/posts` · `PUT /api/posts/{id}` · `DELETE /api/posts/{id}` |
| Publish | `POST /api/posts/{id}/publish` |
| Threaded comments | `GET /api/posts/{postId}/comments/threaded` |
| Add comment | `POST /api/posts/{postId}/comments` |
| Delete comment | `DELETE /api/comments/{id}` |
| Like post / comment | `POST /api/posts/{id}/like` · `POST /api/comments/{id}/like` |
| Follow / Counts | `POST /api/users/{id}/follow` · `GET /api/users/{id}/follows/counts` |
| User profile | `GET /api/users/{username}` · `GET /api/users/me/profile` |
| Update profile | `PUT /api/users/me/profile` |
| Tags / Categories | `GET /api/taxonomy/tags` · `GET /api/taxonomy/categories` |

---

## ⚡ Angular 21 Features Used

| Feature | Usage |
|---|---|
| **Standalone Components** | Every component — zero NgModules |
| **Signals** | `signal()`, `computed()` for all reactive state |
| **`@if` / `@for`** | New control flow — no `*ngIf` / `*ngFor` anywhere |
| **`loadComponent`** | Dynamic per-route code splitting |
| **Functional Guards** | `CanActivateFn` with `inject()` |
| **Functional Interceptor** | `HttpInterceptorFn` with `inject()` |
| **`withFetch()`** | Native Fetch API for HTTP |
| **`withInMemoryScrolling`** | Scroll-to-top on navigation |

---

## 🔐 Auth + Role Access

| Role | Permissions |
|---|---|
| **Reader** | Browse, like, comment, follow |
| **Blogger** | Reader + create / edit / delete own posts |
| **Admin** | Blogger + manage tags & categories |

**Login Modal:** Appears instead of redirecting when any protected action is attempted.

---

## 🎨 Customisation

| What | Where |
|---|---|
| API URL | `src/environments/environment.ts` → `apiUrl` |
| Brand name | Search `inkwell` in templates |
| Fonts | `src/index.html` → Google Fonts import |
| Colours | `src/styles.css` → `:root` CSS variables |
| Page size | Each page/component → `readonly pageSize = 9` |

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, forkJoin, map, of, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { UserAdminDto, AdminStatsDto, AdminPostDto, AdminCommentDto } from '../models/admin.models';
import { TagDto, CategoryDto } from '../models/blog.models';
import { PagedResponseDto } from '../models/post.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── Stats: derived from parallel calls ────────────────────
  getStats(): Observable<AdminStatsDto> {
    const safe = <T>(obs: Observable<T>, fallback: T) => obs.pipe(catchError(e => { console.error(e); return of(fallback); }));
    return forkJoin({
      users:        safe(this.getAllUsers(), [] as UserAdminDto[]),
      postStats:    safe(this.http.get<{ total: number; published: number; draft: number; pending: number }>(`${this.base}/posts/admin/stats`), { total: 0, published: 0, draft: 0, pending: 0 }),
      commentStats: safe(this.http.get<{ total: number; pending: number }>(`${this.base}/admin/comments/stats`), { total: 0, pending: 0 }),
      tags:         safe(this.getTags(), [] as TagDto[]),
      categories:   safe(this.getCategories(), [] as CategoryDto[])
    }).pipe(
      map(r => {
        const users = r.users;
        return {
          totalUsers:      users.length,
          totalBloggers:   users.filter(u => u.role === 'Blogger').length,
          totalReaders:    users.filter(u => u.role === 'Reader').length,
          totalAdmins:     users.filter(u => u.role === 'Admin').length,
          totalPosts:      r.postStats.total,
          publishedPosts:  r.postStats.published,
          draftPosts:      r.postStats.draft,
          pendingPosts:    r.postStats.pending,
          totalComments:   r.commentStats.total,
          pendingComments: r.commentStats.pending,
          totalTags:       r.tags.length,
          totalCategories: r.categories.length
        } as AdminStatsDto;
      }),
      catchError(e => throwError(() => e))
    );
  }

  // ── Users — GET /api/users/admin/all ─────────────────────
  getAllUsers(): Observable<UserAdminDto[]> {
    return this.http.get<UserAdminDto[]>(`${this.base}/users/admin/all`)
      .pipe(catchError(e => throwError(() => e)));
  }

  changeRole(userId: string, role: string): Observable<any> {
    return this.http.put(`${this.base}/users/admin/${userId}/role`, { role })
      .pipe(catchError(e => throwError(() => e)));
  }

  suspendUser(userId: string, suspend: boolean): Observable<any> {
    return this.http.put(`${this.base}/users/admin/${userId}/suspend`, { suspend })
      .pipe(catchError(e => throwError(() => e)));
  }

  setCommentBan(userId: string, canComment: boolean): Observable<any> {
    return this.http.put(`${this.base}/users/admin/${userId}/comment-ban`, { canComment })
      .pipe(catchError(e => throwError(() => e)));
  }

  deleteUser(userId: string): Observable<any> {
    return this.http.delete(`${this.base}/users/admin/${userId}`)
      .pipe(catchError(e => throwError(() => e)));
  }

  // ── Posts — GET /api/posts/admin/pending ─────────────────
  getPendingPosts(page = 1, pageSize = 20): Observable<PagedResponseDto<AdminPostDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponseDto<AdminPostDto>>(`${this.base}/posts/admin/pending`, { params })
      .pipe(catchError(e => throwError(() => e)));
  }

  getAllPosts(page = 1, pageSize = 20, q?: string, visibility?: string): Observable<PagedResponseDto<AdminPostDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (q) params = params.set('q', q);
    if (visibility && visibility !== 'all') params = params.set('visibility', visibility);
    return this.http.get<PagedResponseDto<AdminPostDto>>(`${this.base}/posts/admin/all`, { params })
      .pipe(catchError(e => throwError(() => e)));
  }

  approvePost(postId: string): Observable<any> {
    return this.http.put(`${this.base}/posts/${postId}/approve`, {})
      .pipe(catchError(e => throwError(() => e)));
  }

  rejectPost(postId: string): Observable<any> {
    return this.http.put(`${this.base}/posts/${postId}/reject`, {})
      .pipe(catchError(e => throwError(() => e)));
  }

  adminDeletePost(postId: string): Observable<any> {
    return this.http.delete(`${this.base}/posts/${postId}/admin-delete`)
      .pipe(catchError(e => throwError(() => e)));
  }

  // ── Comments — GET /api/admin/pending ────────────────────
  getPendingComments(page = 1, pageSize = 20): Observable<{ total: number; items: AdminCommentDto[] }> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<{ total: number; items: AdminCommentDto[] }>(`${this.base}/admin/pending`, { params })
      .pipe(catchError(e => throwError(() => e)));
  }

  approveComment(commentId: string): Observable<any> {
    return this.http.put(`${this.base}/admin/${commentId}/approve`, {})
      .pipe(catchError(e => throwError(() => e)));
  }

  adminDeleteComment(commentId: string): Observable<any> {
    return this.http.delete(`${this.base}/admin/${commentId}`)
      .pipe(catchError(e => throwError(() => e)));
  }

  // ── Audit Logs ───────────────────────────────────────────
  getAuditLogs(filter: Partial<import('../models/audit-log.models').AuditLogFilterDto>): Observable<any> {
    let params = new HttpParams()
      .set('page', filter.page ?? 1)
      .set('pageSize', filter.pageSize ?? 20);
    if (filter.action)     params = params.set('action', filter.action);
    if (filter.entityName) params = params.set('entityName', filter.entityName);
    if (filter.status)     params = params.set('status', filter.status);
    if (filter.from)       params = params.set('from', filter.from);
    if (filter.to)         params = params.set('to', filter.to);
    if (filter.userId)     params = params.set('userId', filter.userId);
    return this.http.get<any>(`${this.base}/auditlogs`, { params })
      .pipe(catchError(e => throwError(() => e)));
  }
  getTags(): Observable<TagDto[]> {
    return this.http.get<TagDto[]>(`${this.base}/taxonomy/tags`)
      .pipe(catchError(e => throwError(() => e)));
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.base}/taxonomy/categories`)
      .pipe(catchError(e => throwError(() => e)));
  }

  createTag(name: string): Observable<TagDto> {
    return this.http.post<TagDto>(`${this.base}/taxonomy/tags`, { name })
      .pipe(catchError(e => throwError(() => e)));
  }

  renameTag(id: number, name: string): Observable<TagDto> {
    return this.http.put<TagDto>(`${this.base}/taxonomy/tags/${id}/rename`, { name })
      .pipe(catchError(e => throwError(() => e)));
  }

  deleteTag(id: number): Observable<any> {
    return this.http.delete(`${this.base}/taxonomy/tags/${id}`)
      .pipe(catchError(e => throwError(() => e)));
  }

  createCategory(name: string): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(`${this.base}/taxonomy/categories`, { name })
      .pipe(catchError(e => throwError(() => e)));
  }

  renameCategory(id: number, name: string): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(`${this.base}/taxonomy/categories/${id}/rename`, { name })
      .pipe(catchError(e => throwError(() => e)));
  }

  deleteCategory(id: number): Observable<any> {
    return this.http.delete(`${this.base}/taxonomy/categories/${id}`)
      .pipe(catchError(e => throwError(() => e)));
  }
}

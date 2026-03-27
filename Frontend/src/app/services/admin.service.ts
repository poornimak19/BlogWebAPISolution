import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, forkJoin, map, throwError } from 'rxjs';
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
    return forkJoin({
      users:      this.getAllUsers(),
      posts:      this.getPendingPosts(1, 1),
      comments:   this.getPendingComments(1, 1),
      tags:       this.getTags(),
      categories: this.getCategories()
    }).pipe(
      map(r => {
        const users = r.users;
        return {
          totalUsers:      users.length,
          totalBloggers:   users.filter(u => u.role === 'Blogger').length,
          totalReaders:    users.filter(u => u.role === 'Reader').length,
          totalAdmins:     users.filter(u => u.role === 'Admin').length,
          totalPosts:      0,
          publishedPosts:  0,
          draftPosts:      0,
          pendingPosts:    r.posts.total,
          totalComments:   0,
          pendingComments: r.comments.total,
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

  // ── Taxonomy ─────────────────────────────────────────────
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

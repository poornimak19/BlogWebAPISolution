import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { PostDetailDto, PostSummaryDto, CreatePostRequestDto, UpdatePostRequestDto, PagedResponseDto } from '../models/post.models';

@Injectable({ providedIn: 'root' })
export class PostService {
  private readonly base = `${environment.apiUrl}/posts`;

  constructor(private http: HttpClient) {}

  getPublished(
  page = 1,
  pageSize = 9,
  q?: string,
  tag?: string,
  category?: string
): Observable<PagedResponseDto<PostSummaryDto>> {

  let params = new HttpParams()
    .set('page', page)
    .set('pageSize', pageSize);

  if (q) params = params.set('q', q);
  if (tag) params = params.set('tag', tag);
  if (category) params = params.set('category', category);

  // ✅ Send logged-in user ID to backend
  const userId = localStorage.getItem('userId');      // OR your authService.getUserId()

  if (userId) {
    params = params.set('currentUserId', userId);
  }

  return this.http.get<PagedResponseDto<PostSummaryDto>>(
    `${this.base}/published`,
    { params }
  );
}

  getBySlug(slug: string): Observable<PostDetailDto> {
    return this.http.get<PostDetailDto>(`${this.base}/slug/${slug}`)
      .pipe(catchError(e => throwError(() => e)));
  }

  getMyPosts(page = 1, pageSize = 100): Observable<PagedResponseDto<PostSummaryDto>> {
    return this.http.get<PagedResponseDto<PostSummaryDto>>(`${this.base}/mine`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    }).pipe(catchError(e => throwError(() => e)));
  }

  getById(id: string): Observable<PostDetailDto> {
    return new Observable(obs => {
      this.getMyPosts(1, 100).subscribe({
        next: res => {
          const match = res.items.find(p => p.id === id);
          if (!match) { obs.error({ status: 404 }); return; }
          this.getBySlug(match.slug).subscribe({
            next: d => { obs.next(d); obs.complete(); },
            error: e => obs.error(e)
          });
        },
        error: e => obs.error(e)
      });
    });
  }

  create(dto: CreatePostRequestDto): Observable<PostDetailDto> {
    return this.http.post<PostDetailDto>(this.base, dto).pipe(catchError(e => throwError(() => e)));
  }

  update(id: string, dto: UpdatePostRequestDto): Observable<PostDetailDto> {
    return this.http.put<PostDetailDto>(`${this.base}/${id}`, dto).pipe(catchError(e => throwError(() => e)));
  }

  publish(id: string): Observable<any> {
    return this.http.post(`${this.base}/${id}/publish`, {}).pipe(catchError(e => throwError(() => e)));
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.base}/${id}`).pipe(catchError(e => throwError(() => e)));
  }
}

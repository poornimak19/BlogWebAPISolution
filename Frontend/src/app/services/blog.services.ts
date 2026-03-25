import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

import {
  CommentDto, CreateCommentRequestDto, UpdateCommentRequestDto, ThreadedComment,
  ReactionResponseDto, FollowToggleResponseDto, FollowCountsDto,
  TagDto, CategoryDto, UserProfileDto, UpdateUserProfileDto,UserSearchDto
} from '../models/blog.models';
import { PagedResponseDto } from '../models/post.models';

@Injectable({ providedIn: 'root' })
export class CommentService {
  private readonly base = environment.apiUrl;
  constructor(private http: HttpClient) {}

  getThreaded(postId: string, page = 1, pageSize = 20): Observable<ThreadedComment[]> {
    return this.http.get<ThreadedComment[]>(`${this.base}/posts/${postId}/comments/threaded`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    }).pipe(catchError(e => throwError(() => e)));
  }

  add(postId: string, dto: CreateCommentRequestDto): Observable<CommentDto> {
    return this.http.post<CommentDto>(`${this.base}/posts/${postId}/comments`, dto)
      .pipe(catchError(e => throwError(() => e)));
  }

  update(id: string, dto: UpdateCommentRequestDto): Observable<CommentDto> {
    return this.http.put<CommentDto>(`${this.base}/comments/${id}`, dto)
      .pipe(catchError(e => throwError(() => e)));
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.base}/comments/${id}`).pipe(catchError(e => throwError(() => e)));
  }
}

@Injectable({ providedIn: 'root' })
export class ReactionService {
  private readonly base = environment.apiUrl;
  constructor(private http: HttpClient) {}

  togglePostLike(postId: string): Observable<ReactionResponseDto> {
    return this.http.post<ReactionResponseDto>(`${this.base}/posts/${postId}/like`, {})
      .pipe(catchError(e => throwError(() => e)));
  }

  toggleCommentLike(commentId: string): Observable<ReactionResponseDto> {
    return this.http.post<ReactionResponseDto>(`${this.base}/comments/${commentId}/like`, {})
      .pipe(catchError(e => throwError(() => e)));
  }
}

@Injectable({ providedIn: 'root' })
export class FollowService {
  private readonly base = `${environment.apiUrl}/users`;
  constructor(private http: HttpClient) {}

  toggle(userId: string): Observable<FollowToggleResponseDto> {
    return this.http.post<FollowToggleResponseDto>(`${this.base}/${userId}/follow`, {})
      .pipe(catchError(e => throwError(() => e)));
  }

  getCounts(userId: string): Observable<FollowCountsDto> {
    return this.http.get<FollowCountsDto>(`${this.base}/${userId}/follows/counts`)
      .pipe(catchError(e => throwError(() => e)));
  }
}

@Injectable({ providedIn: 'root' })
export class TaxonomyService {
  private readonly base = `${environment.apiUrl}/taxonomy`;
  constructor(private http: HttpClient) {}

  getTags(): Observable<TagDto[]> {
    return this.http.get<TagDto[]>(`${this.base}/tags`).pipe(catchError(e => throwError(() => e)));
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(`${this.base}/categories`).pipe(catchError(e => throwError(() => e)));
  }
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly base = `${environment.apiUrl}/users`;
  constructor(private http: HttpClient) {}

  getByUsername(username: string): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.base}/${username}`).pipe(catchError(e => throwError(() => e)));
  }

  getMyProfile(): Observable<UserProfileDto> {
    return this.http.get<UserProfileDto>(`${this.base}/me/profile`).pipe(catchError(e => throwError(() => e)));
  }

  updateProfile(dto: UpdateUserProfileDto): Observable<UserProfileDto> {
    return this.http.put<UserProfileDto>(`${this.base}/me/profile`, dto).pipe(catchError(e => throwError(() => e)));
  }

  searchUsers(q: string): Observable<UserSearchDto[]> {
  return this.http.get<UserSearchDto[]>(`${this.base}/search`, { params: new HttpParams().set('q', q) } ).pipe(catchError(e => throwError(() => e)));
}
}

// Matches backend PostDTOs exactly
export interface AuthorSummaryDto {
  id: string;
  username: string;
  displayName?: string;
  avatarUrl?: string;
}

export interface PostSummaryDto {
  id: string;
  title: string;
  slug: string;
  excerpt?: string;
  coverImageUrl?: string;
  status: string;
  visibility: string;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  author: AuthorSummaryDto;
  tags: string[];
  categories: string[];
}

export interface PostDetailDto {
  id: string;
  title: string;
  slug: string;
  excerpt?: string;
  contentHtml: string;
  contentMarkdown?: string;
  coverImageUrl?: string;
  status: string;
  visibility: string;
  commentsEnabled: boolean;
  autoApproveComments: boolean;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  author: AuthorSummaryDto;
  tags: string[];
  categories: string[];
  allowedAudienceUserIds: string[];
}

export interface CreatePostRequestDto {
  title: string;
  slug?: string;
  excerpt?: string;
  contentHtml: string;
  contentMarkdown?: string;
  visibility: 'Public' | 'Private' | 'Restricted';
  tagNames?: string[];
  categoryNames?: string[];
  allowedUserIds?: string[];
  commentsEnabled?: boolean;
  autoApproveComments?: boolean;
  coverImageUrl?: string;              //coverimage
}

export interface UpdatePostRequestDto {
  title?: string;
  slug?: string;
  excerpt?: string;
  contentHtml?: string;
  contentMarkdown?: string;
  visibility?: string;
  tagNames?: string[];
  categoryNames?: string[];
  allowedUserIds?: string[];
  commentsEnabled?: boolean;
  autoApproveComments?: boolean;
  status?: string;
  coverImageUrl?: string | null;           //coverimage
}

export interface PagedResponseDto<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

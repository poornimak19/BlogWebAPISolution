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
  audioUrl?: string;
  videoUrl?: string;
  isPremium: boolean;
  status: string;
  visibility: string;
  isRejected: boolean;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  author: AuthorSummaryDto;
  tags: string[];
  categories: string[];
  likesCount: number;
}

export interface PostDetailDto {
  id: string;
  title: string;
  slug: string;
  excerpt?: string;
  contentHtml: string;
  contentMarkdown?: string;
  coverImageUrl?: string;
  audioUrl?: string;
  videoUrl?: string;
  isPremium: boolean;
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
  likesCount: number;
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
  coverImageUrl?: string;
  audioUrl?: string;
  videoUrl?: string;
  isPremium?: boolean;
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
  coverImageUrl?: string | null;
  audioUrl?: string | null;
  videoUrl?: string | null;
  isPremium?: boolean;
}

export interface PagedResponseDto<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

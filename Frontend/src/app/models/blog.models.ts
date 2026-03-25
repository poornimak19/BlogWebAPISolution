// Matches backend CommentDTOs, ReactionDTOs, FollowDTOs, TaxonomyDTOs, UserDTOs exactly

export interface CommentAuthorDto {
  id: string;
  username: string;
  displayName?: string;
  avatarUrl?: string;
}

export interface CommentDto {
  id: string;
  postId: string;
  parentCommentId?: string;
  author?: CommentAuthorDto;
  content: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  repliesCount: number;
}

export interface CreateCommentRequestDto {
  content: string;
  parentCommentId?: string;
}

export interface UpdateCommentRequestDto {
  content?: string;
  status?: string;
}

export interface ThreadedComment {
  parent: CommentDto;
  replies: CommentDto[];
}

export interface ReactionResponseDto {
  liked: boolean;
  totalLikes: number;
}

export interface FollowToggleResponseDto {
  following: boolean;
  followersCount: number;
}

export interface FollowCountsDto {
  followers: number;
  following: number;
}

export interface TagDto {
  id: number;
  name: string;
  slug: string;
}

export interface CategoryDto {
  id: number;
  name: string;
  slug: string;
}

export interface UserProfileDto {
  id: string;
  username: string;
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
  followers: number;
  following: number;
  role?: string;   // 'Reader' | 'Blogger' | 'Admin' — returned by /api/users/me/profile
}

export interface UpdateUserProfileDto {
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
}


export interface UserSearchDto {
  id: string;
  username: string;
  displayName?: string;
  avatarUrl?: string;
}
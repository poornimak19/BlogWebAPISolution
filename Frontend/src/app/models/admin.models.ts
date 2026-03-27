// Matches backend AdminDTO.cs exactly

export interface UserAdminDto {
  id: string;
  username: string;
  email: string;
  displayName?: string;
  role: string;
  isSuspended: boolean;
  canComment: boolean;
  createdAt: string;
}

export interface AdminStatsDto {
  totalUsers: number;
  totalBloggers: number;
  totalReaders: number;
  totalAdmins: number;
  totalPosts: number;
  publishedPosts: number;
  draftPosts: number;
  pendingPosts: number;
  totalComments: number;
  pendingComments: number;
  totalTags: number;
  totalCategories: number;
}

export interface AdminPostDto {
  id: string;
  title: string;
  slug: string;
  status: string;
  visibility: string;
  isRejected: boolean;
  coverImageUrl?: string;
  createdAt: string;
  updatedAt: string;
  author: {
    id: string;
    username: string;
    displayName?: string;
  };
  tags: string[];
  categories: string[];
}

export interface AdminCommentDto {
  id: string;
  content: string;
  status: string;
  createdAt: string;
  postId: string;
  postTitle?: string;
  author?: {
    id: string;
    username: string;
    displayName?: string;
  };
}

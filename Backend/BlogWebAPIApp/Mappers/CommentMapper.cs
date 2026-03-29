using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;

namespace BlogWebAPIApp.Mappers
{

    public static class CommentMappers
    {
        public static CommentAuthorDto? ToAuthorDto(this User? u)
            => u == null ? null : new CommentAuthorDto(u.Id, u.Username, u.DisplayName, u.AvatarUrl);

        public static CommentDto ToDto(this Comment c, int repliesCount = 0)
            => new CommentDto(
                Id: c.Id,
                PostId: c.PostId,
                ParentCommentId: c.ParentCommentId,
                Author: c.Author.ToAuthorDto(),
                Content: c.Content,
                Status: c.Status.ToString(),
                CreatedAt: c.CreatedAt,
                UpdatedAt: c.UpdatedAt,
                RepliesCount: repliesCount,
                PostTitle: c.Post?.Title
            );
    }

}

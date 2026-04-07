using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;
using System.Linq;
namespace BlogWebAPIApp.Mappers
{

    public static class PostMappers
    {
        public static AuthorSummaryDto ToAuthorSummaryDto(this User u) =>
            new AuthorSummaryDto(u.Id, u.Username, u.DisplayName, u.AvatarUrl);

        public static PostSummaryDto ToSummaryDto(this Post p) =>
            new PostSummaryDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.CoverImageUrl,
                p.AudioUrl,
                p.VideoUrl,
                p.IsPremium,
                p.Status.ToString(),
                p.Visibility.ToString(),
                p.IsRejected,
                p.PublishedAt,
                p.CreatedAt,
                p.UpdatedAt,
                p.Author != null? p.Author.ToAuthorSummaryDto():new AuthorSummaryDto(Guid.Empty, "(unknown)", null, null),
                p.PostTags.Select(t => t.Tag.Slug).ToList(),
                p.PostCategories.Select(c => c.Category.Slug).ToList(),
                p.Likes.Count
            );

        public static PostDetailDto ToDetailDto(this Post p) =>
            new PostDetailDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Excerpt,
                p.ContentHtml,
                p.ContentMarkdown,
                p.CoverImageUrl,
                p.AudioUrl,
                p.VideoUrl,
                p.IsPremium,
                p.Status.ToString(),
                p.Visibility.ToString(),
                p.CommentsEnabled,
                p.AutoApproveComments,
                p.PublishedAt,
                p.CreatedAt,
                p.UpdatedAt,
                p.Author != null? p.Author.ToAuthorSummaryDto(): new AuthorSummaryDto(Guid.Empty, "(unknown)", null, null),
                p.PostTags.Select(t => t.Tag.Slug).ToList(),
                p.PostCategories.Select(c => c.Category.Slug).ToList(),
                p.Audience.Select(a => a.UserId).ToList(),
                p.Likes.Count
            );
    }

}

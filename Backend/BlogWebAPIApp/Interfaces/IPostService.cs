using BlogWebAPIApp.Models;

namespace BlogWebAPIApp.Interfaces
{

    public interface IPostService
    {
        Task<Post> Create(Guid authorId,
                          string title,
                          string? slug,
                          string? excerpt,
                          string contentHtml,
                          string? contentMarkdown,
                          string visibility,                    // "Public" | "Private" | "Restricted"
                          IEnumerable<string>? tagNames,
                          IEnumerable<string>? categoryNames,
                          IEnumerable<Guid>? allowedUserIds,
                          bool? commentsEnabled,
                          bool? autoApproveComments,
                          string coverImage);

        Task<Post?> GetById(Guid postId);
        Task<Post?> GetBySlug(string slug);
        Task<Post> Update(Guid postId,
                          Guid actorUserId,
                          string? title,
                          string? slug,
                          string? excerpt,
                          string? contentHtml,
                          string? contentMarkdown,
                          string? visibility,                  // optional
                          IEnumerable<string>? tagNames,
                          IEnumerable<string>? categoryNames,
                          IEnumerable<Guid>? allowedUserIds,
                          bool? commentsEnabled,
                          bool? autoApproveComments,
                          string? status);                     // "Draft" | "Published" | "Archived"

        Task Publish(Guid postId, Guid actorUserId);
        Task Delete(Guid postId, Guid actorUserId);

        Task<(IReadOnlyList<Post> items, int total)> GetPublished(int page, int pageSize, string? q, string? tagSlug, string? categorySlug);
        Task<(IReadOnlyList<Post> items, int total)> GetByAuthor(Guid authorId, int page, int pageSize);
    }

}

using BlogWebAPIApp.Models;

namespace BlogWebAPIApp.Interfaces
{

    public interface ISearchService
    {
        Task<(IReadOnlyList<Post> items, int total)> SearchPosts(int page, int pageSize,
                                                                 string? q,
                                                                 string? tagSlug,
                                                                 string? categorySlug,
                                                                 string? authorUsername,
                                                                 string? sort); // "recent" | "popular"
    }

}

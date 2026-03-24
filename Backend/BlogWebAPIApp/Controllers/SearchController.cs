using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Mappers;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogWebAPIApp.Controllers
{

    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _search;
        private const int MAX_PAGE_SIZE = 50;

        public SearchController(ISearchService search)
        {
            _search = search;
        }

        /// <summary>
        /// Search published, public posts with optional filters.
        /// </summary>
        /// <param name="page">1-based page index</param>
        /// <param name="pageSize">page size (max 50)</param>
        /// <param name="q">full-text query (title/content)</param>
        /// <param name="tag">filter by tag slug</param>
        /// <param name="category">filter by category slug</param>
        /// <param name="author">filter by author username</param>
        /// <param name="sort">"recent" (default) or "popular"</param>
        
        #region Get Posts - Public
        [AllowAnonymous]
        [HttpGet("posts")]
        public async Task<ActionResult<PagedResponseDto<PostSummaryDto>>> SearchPosts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? q = null,
            [FromQuery(Name = "tag")] string? tag = null,
            [FromQuery(Name = "category")] string? category = null,
            [FromQuery(Name = "author")] string? author = null,
            [FromQuery] string? sort = "recent")
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(new { message = "page and pageSize must be positive" });

            pageSize = Math.Min(pageSize, MAX_PAGE_SIZE);

            var (items, total) = await _search.SearchPosts(
                page: page,
                pageSize: pageSize,
                q: q,
                tagSlug: tag,
                categorySlug: category,
                authorUsername: author,
                sort: sort
            );

            var payload = new PagedResponseDto<PostSummaryDto>(
                Items: items.Select(p => p.ToSummaryDto()).ToList(),
                Total: total,
                Page: page,
                PageSize: pageSize
            );

            return Ok(payload);
        }
        #endregion
    }
}

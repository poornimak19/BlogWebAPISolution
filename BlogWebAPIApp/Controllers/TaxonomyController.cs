using BlogWebAPIApp.Interfaces;
using BlogWebAPIApp.Mappers;
using BlogWebAPIApp.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogWebAPIApp.Controllers
{

    [ApiController]
    [Route("api/taxonomy")]
    public class TaxonomyController : ControllerBase
    {
        private readonly ITaxonomyService _taxonomy;

        public TaxonomyController(ITaxonomyService taxonomy)
        {
            _taxonomy = taxonomy;
        }

        // PUBLIC: Get all tags
        #region Get tags
        [AllowAnonymous]
        [HttpGet("tags")]
        public async Task<ActionResult<IReadOnlyList<TagDto>>> GetAllTags()
        {
            var items = await _taxonomy.GetAllTags();
            return Ok(items.Select(t => t.ToDto()).ToList());
        }
        #endregion

        // PUBLIC: Get all categories
        #region Get categories
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAllCategories()
        {
            var items = await _taxonomy.GetAllCategories();
            return Ok(items.Select(c => c.ToDto()).ToList());
        }
        #endregion

        // ADMIN: Ensure a tag exists (create if missing)
        #region Create Tags
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("tags")]
        public async Task<ActionResult<TagDto>> EnsureTag([FromBody] CreateTagRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var tag = await _taxonomy.EnsureTag(dto.Name);
            return Ok(tag.ToDto());
        }
        #endregion


        // ADMIN: Ensure a category exists (create if missing)
        #region Create Category
        [Authorize(Policy = "AdminOnly")]
        [HttpPost("categories")]
        public async Task<ActionResult<CategoryDto>> EnsureCategory([FromBody] CreateCategoryRequestDto dto)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var category = await _taxonomy.EnsureCategory(dto.Name);
            return Ok(category.ToDto());
        }
        #endregion
    }

}

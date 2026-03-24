using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;

namespace BlogWebAPIApp.Mappers
{


    public static class TaxonomyMappers
    {
        public static TagDto ToDto(this Tag t)
            => new TagDto(t.Id, t.Name, t.Slug);

        public static CategoryDto ToDto(this Category c)
            => new CategoryDto(c.Id, c.Name, c.Slug);
    }

}

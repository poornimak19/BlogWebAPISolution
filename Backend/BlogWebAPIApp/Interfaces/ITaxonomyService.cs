using BlogWebAPIApp.Models;

namespace BlogWebAPIApp.Interfaces
{

    public interface ITaxonomyService
    {
        Task<IReadOnlyList<Tag>> GetAllTags();
        Task<IReadOnlyList<Category>> GetAllCategories();
        Task<Tag> EnsureTag(string name);
        Task<Category> EnsureCategory(string name);
        Task<Tag> RenameTag(int id, string newName);
        Task DeleteTag(int id);

        Task<Category> RenameCategory(int id, string newName);
        Task DeleteCategory(int id);
    }

}

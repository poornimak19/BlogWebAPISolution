using BlogWebAPIApp.Models;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Interfaces
{

    public interface IUserService
    {
        Task<User?> GetByUsername(string username);
        Task<User?> GetById(Guid id);
        Task UpdateProfile(Guid userId, string? displayName, string? bio, string? avatarUrl);
        Task<(int followers, int following)> GetFollowCounts(Guid userId);

        Task<IEnumerable<User>> SearchUsers(string query);
        Task<List<User>> GetAllUsers();
        Task ChangeRole(Guid userId, UserRole newRole);
        Task SuspendUser(Guid userId, bool suspend);
        Task DeleteUser(Guid userId);
        Task SetCommentPermission(Guid userId, bool canComment);
        
    }

}

using BlogWebAPIApp.Models;

namespace BlogWebAPIApp.Interfaces
{

    public interface IUserService
    {
        Task<User?> GetByUsername(string username);
        Task<User?> GetById(Guid id);
        Task UpdateProfile(Guid userId, string? displayName, string? bio, string? avatarUrl);
        Task<(int followers, int following)> GetFollowCounts(Guid userId);
    }

}

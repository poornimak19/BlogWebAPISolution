namespace BlogWebAPIApp.Interfaces
{

    public interface IFollowService
    {
        Task<(bool following, int followersCount)> ToggleFollow(Guid followerId, Guid followeeId);
        Task<(int followers, int following)> GetCounts(Guid userId);
    }

}

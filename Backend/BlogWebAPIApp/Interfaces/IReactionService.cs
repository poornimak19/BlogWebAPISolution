namespace BlogWebAPIApp.Interfaces
{

    public interface IReactionService
    {
        Task<(bool liked, int totalLikes)> TogglePostLike(Guid postId, Guid userId);
        Task<(bool liked, int totalLikes)> ToggleCommentLike(Guid commentId, Guid userId);
    }

}

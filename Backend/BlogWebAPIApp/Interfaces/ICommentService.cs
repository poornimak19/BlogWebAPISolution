using BlogWebAPIApp.Models;

namespace BlogWebAPIApp.Interfaces
{

    public interface ICommentService
    {
        Task<Comment> Add(Guid postId, Guid authorId, string content, Guid? parentCommentId);
        Task<Comment?> GetById(Guid commentId);
        Task Update(Guid commentId, Guid actorUserId, string content, string? status); // status: "Pending"|"Approved"|"Removed"
        Task Delete(Guid commentId, Guid actorUserId);
        Task<(IReadOnlyList<Comment> items, int total)> GetByPost(Guid postId, int page, int pageSize);
        Task<(IReadOnlyList<Comment> items, int total)> GetPendingComments(int page, int pageSize);
        Task AdminApprove(Guid commentId);
        Task AdminDelete(Guid commentId);
    }

}

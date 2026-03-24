using BlogWebAPIApp.Models;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Interfaces
{

    public interface IAuthService
    {
        Task<(User user, string token)> Register(string email, string username, string password, string? displayName, UserRole role);
        Task<(User user, string token)> Login(string emailOrUsername, string password);


        Task<string> ForgotPassword(string email);
        Task ResetPassword(string token, string newPassword);

    }

}

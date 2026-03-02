using System.Security.Claims;

namespace BlogWebAPIApp.Extensions
{

    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Tries to resolve current user's Guid from NameIdentifier or 'sub'.
        /// Returns null if unauthenticated or invalid guid.
        /// </summary>
        public static Guid? GetUserId(this ClaimsPrincipal user)
        {
            if (user?.Identity?.IsAuthenticated != true) return null;

            var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
            return Guid.TryParse(id, out var gid) ? gid : null;
        }
    }

}

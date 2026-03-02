using BlogWebAPIApp.Models;
using BlogWebAPIApp.Models.DTOs;

namespace BlogWebAPIApp.Mappers
{


    public static class UserMappers
    {
        public static UserProfileDto ToProfileDto(
            this User u,
            int followers,
            int following)
        {
            return new UserProfileDto(
                u.Id,
                u.Username,
                u.DisplayName,
                u.Bio,
                u.AvatarUrl,
                followers,
                following
            );
        }
    }

}

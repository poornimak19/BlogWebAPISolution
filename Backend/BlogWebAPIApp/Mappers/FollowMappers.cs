using BlogWebAPIApp.Models.DTOs;

namespace BlogWebAPIApp.Mappers
{

    public static class FollowMappers
    {
        public static FollowToggleResponseDto ToToggleDto(this (bool following, int followersCount) r)
            => new FollowToggleResponseDto(r.following, r.followersCount);

        public static FollowCountsDto ToCountsDto(this (int followers, int following) r)
            => new FollowCountsDto(r.followers, r.following);
    }

}

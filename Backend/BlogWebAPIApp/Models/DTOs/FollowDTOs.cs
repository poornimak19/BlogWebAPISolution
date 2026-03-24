namespace BlogWebAPIApp.Models.DTOs
{

    public record FollowToggleResponseDto(
            bool Following,
            int FollowersCount
        );

    public record FollowCountsDto(
        int Followers,
        int Following
    );

}

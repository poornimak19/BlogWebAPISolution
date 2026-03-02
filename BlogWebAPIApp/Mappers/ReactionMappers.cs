using BlogWebAPIApp.Models.DTOs;

namespace BlogWebAPIApp.Mappers
{

    public static class ReactionMappers
    {
        public static ReactionResponseDto ToDto(this (bool liked, int totalLikes) result)
            => new ReactionResponseDto(result.liked, result.totalLikes);
    }

}

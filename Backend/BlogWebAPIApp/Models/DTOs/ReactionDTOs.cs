namespace BlogWebAPIApp.Models.DTOs
{

    public record ReactionResponseDto(
           bool Liked,
           int TotalLikes
       );

}

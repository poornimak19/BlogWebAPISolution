namespace BlogWebAPIApp.Models.DTOs
{
    public class ResetPasswordRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
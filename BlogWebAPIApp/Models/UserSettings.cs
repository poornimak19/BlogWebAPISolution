namespace BlogWebAPIApp.Models
{
    public class UserSettings
    {

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public bool DefaultCommentsEnabled { get; set; } = true;
        public bool DefaultAutoApproveComments { get; set; } = true;

        

    }
}

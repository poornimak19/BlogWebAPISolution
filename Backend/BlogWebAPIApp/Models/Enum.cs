namespace BlogWebAPIApp.Models
{
    public class Enum
    {

        public enum UserRole { Reader = 0, Blogger = 1, Admin = 2 }

        
        public enum PostStatus { Draft = 0, Published = 1, Archived = 2 }

        public enum Visibility { Public = 0, Private = 1, Restricted = 2 }

        public enum CommentStatus { Pending = 0, Approved = 1, Removed = 2 }

        public enum ReportStatus { Open = 0, Resolved = 1, Dismissed = 2 }

        public enum ReportTargetType { Post = 0, Comment = 1 }

    }
}

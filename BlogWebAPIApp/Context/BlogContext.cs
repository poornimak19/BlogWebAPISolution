using BlogWebAPIApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using static BlogWebAPIApp.Models.Enum;

namespace BlogWebAPIApp.Context
{
    public class BlogContext : DbContext
    {

        public BlogContext(DbContextOptions<BlogContext> options) : base(options) { }

        // DbSets
        public DbSet<ErrorLog> Logs => Set<ErrorLog>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserSettings> UserSettings => Set<UserSettings>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<PostTag> PostTags => Set<PostTag>();
        public DbSet<PostCategory> PostCategories => Set<PostCategory>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<PostLike> PostLikes => Set<PostLike>();
        public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
        public DbSet<Follow> Follows => Set<Follow>();
        public DbSet<UserInterest> UserInterests => Set<UserInterest>();
        public DbSet<PostAudience> PostAudiences => Set<PostAudience>();
        public DbSet<Report> Reports => Set<Report>();




        protected override void OnModelCreating(ModelBuilder b)
        {
            // ---------- Users ----------
            b.Entity<User>(e =>
            {
                e.HasIndex(x => x.Email).IsUnique();
                e.HasIndex(x => x.Username).IsUnique();
                e.Property(x => x.Status).HasMaxLength(16);

                e.HasOne(x => x.Settings)
                    .WithOne(s => s.User)
                    .HasForeignKey<UserSettings>(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // safe
            });

            b.Entity<UserSettings>(e =>
            {
                e.HasKey(s => s.UserId);
            });

            // ---------- Posts ----------
            b.Entity<Post>(e =>
            {
                e.HasIndex(x => x.Slug).IsUnique();
                e.Property(x => x.ContentHtml).HasColumnType("nvarchar(max)");
                e.Property(x => x.ContentMarkdown).HasColumnType("nvarchar(max)");

                // user -> posts: cascade is OK (single path)
                e.HasOne(p => p.Author)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Tags / Categories ----------
            b.Entity<Tag>(e =>
            {
                e.HasIndex(x => x.Name).IsUnique();
                e.HasIndex(x => x.Slug).IsUnique();
            });

            b.Entity<Category>(e =>
            {
                e.HasIndex(x => x.Name).IsUnique();
                e.HasIndex(x => x.Slug).IsUnique();
            });

            // ---------- PostTag (M:N) ----------
            b.Entity<PostTag>(e =>
            {
                e.HasKey(pt => new { pt.PostId, pt.TagId });
                e.HasOne(pt => pt.Post)
                    .WithMany(p => p.PostTags)
                    .HasForeignKey(pt => pt.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(pt => pt.Tag)
                    .WithMany(t => t.PostTags)
                    .HasForeignKey(pt => pt.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- PostCategory (M:N) ----------
            b.Entity<PostCategory>(e =>
            {
                e.HasKey(pc => new { pc.PostId, pc.CategoryId });
                e.HasOne(pc => pc.Post)
                    .WithMany(p => p.PostCategories)
                    .HasForeignKey(pc => pc.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(pc => pc.Category)
                    .WithMany(c => c.PostCategories)
                    .HasForeignKey(pc => pc.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Comments ----------
            b.Entity<Comment>(e =>
            {
                // Post -> Comments: cascade (delete post deletes comments)
                e.HasOne(c => c.Post)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> Comments (Author): NO ACTION to avoid second path
                e.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Self reference: NO ACTION
                e.HasOne(c => c.Parent)
                    .WithMany(p => p.Replies)
                    .HasForeignKey(c => c.ParentCommentId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(c => new { c.PostId, c.CreatedAt });
            });

            // ---------- PostLike ----------
            b.Entity<PostLike>(e =>
            {
                e.HasKey(l => new { l.UserId, l.PostId });

                // Break user direct cascade to likes (since Post also cascades to likes)
                e.HasOne(l => l.User)
                    .WithMany(u => u.PostLikes)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Keep cascade from Post -> Likes
                e.HasOne(l => l.Post)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(l => l.PostId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- CommentLike ----------
            b.Entity<CommentLike>(e =>
            {
                e.HasKey(l => new { l.UserId, l.CommentId });

                // Break user direct cascade
                e.HasOne(l => l.User)
                    .WithMany(u => u.CommentLikes)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Keep cascade from Comment -> Likes
                e.HasOne(l => l.Comment)
                    .WithMany(c => c.Likes)
                    .HasForeignKey(l => l.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- Follows (two FKs to Users) ----------
            b.Entity<Follow>(e =>
            {
                e.HasKey(f => new { f.FollowerId, f.FolloweeId });

                e.HasOne(f => f.Follower)
                    .WithMany(u => u.Following)
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.NoAction); // critical

                e.HasOne(f => f.Followee)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.FolloweeId)
                    .OnDelete(DeleteBehavior.NoAction); // critical
            });

            // ---------- UserInterest ----------
            b.Entity<UserInterest>(e =>
            {
                e.HasKey(ui => new { ui.UserId, ui.TagId });

                // Safe to cascade (no other path to UserInterests)
                e.HasOne(ui => ui.User)
                    .WithMany(u => u.Interests)
                    .HasForeignKey(ui => ui.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ui => ui.Tag)
                    .WithMany()
                    .HasForeignKey(ui => ui.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- PostAudience ----------
            b.Entity<PostAudience>(e =>
            {
                e.HasKey(a => new { a.PostId, a.UserId });

                // Keep cascade from Post
                e.HasOne(a => a.Post)
                    .WithMany(p => p.Audience)
                    .HasForeignKey(a => a.PostId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Break user direct cascade
                e.HasOne(a => a.User)
                    .WithMany(u => u.AllowedPostAudiences)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // ---------- Report ----------
            b.Entity<Report>(e =>
            {
                // Avoid cycles in moderation too
                e.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(r => r.ResolvedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ResolvedById)
                    .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(r => new { r.Status, r.CreatedAt });
            });
        }


    }
}

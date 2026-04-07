using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogWebAPIApp.Migrations
{
    /// <inheritdoc />
    public partial class addMediaUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                table: "Posts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Posts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Posts");
        }
    }
}

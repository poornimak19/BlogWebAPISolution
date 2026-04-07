using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlogWebAPIApp.Migrations
{
    /// <inheritdoc />
    public partial class addPremium : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremiumSubscriber",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumExpiresAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Posts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PremiumReadLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumReadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PremiumReadLogs_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PremiumReadLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumReadLogs_PostId",
                table: "PremiumReadLogs",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumReadLogs_UserId_PostId",
                table: "PremiumReadLogs",
                columns: new[] { "UserId", "PostId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PremiumReadLogs");

            migrationBuilder.DropColumn(
                name: "IsPremiumSubscriber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Posts");
        }
    }
}

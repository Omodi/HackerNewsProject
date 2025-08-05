using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HackerNewsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateSearchSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    CommentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Domain = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IndexedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Author",
                table: "Stories",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_CreatedAt",
                table: "Stories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_CreatedAt_Score",
                table: "Stories",
                columns: new[] { "CreatedAt", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Domain",
                table: "Stories",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_IndexedAt",
                table: "Stories",
                column: "IndexedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Stories_Score",
                table: "Stories",
                column: "Score");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stories");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HackerNewsApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFTS5SearchTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing FTS table and triggers
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Stories_ai");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Stories_ad");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Stories_au");
            migrationBuilder.Sql("DROP TABLE IF EXISTS StoriesSearch");

            // Recreate FTS5 table with correct structure
            migrationBuilder.Sql(@"
                CREATE VIRTUAL TABLE StoriesSearch USING fts5(
                    Title,
                    Author,
                    Domain,
                    Score UNINDEXED,
                    CreatedAt UNINDEXED,
                    CommentCount UNINDEXED,
                    HasUrl UNINDEXED,
                    tokenize='porter unicode61'
                )");

            // Recreate triggers
            migrationBuilder.Sql(@"
                CREATE TRIGGER Stories_ai AFTER INSERT ON Stories BEGIN
                    INSERT INTO StoriesSearch(rowid, Title, Author, Domain, Score, CreatedAt, CommentCount, HasUrl)
                    VALUES (new.Id, new.Title, new.Author, COALESCE(new.Domain, ''),
                            new.Score, new.CreatedAt, new.CommentCount,
                            CASE WHEN new.Url IS NOT NULL AND new.Url != '' THEN 1 ELSE 0 END);
                END");

            migrationBuilder.Sql(@"
                CREATE TRIGGER Stories_ad AFTER DELETE ON Stories BEGIN
                    DELETE FROM StoriesSearch WHERE rowid = old.Id;
                END");

            migrationBuilder.Sql(@"
                CREATE TRIGGER Stories_au AFTER UPDATE ON Stories BEGIN
                    DELETE FROM StoriesSearch WHERE rowid = old.Id;
                    INSERT INTO StoriesSearch(rowid, Title, Author, Domain, Score, CreatedAt, CommentCount, HasUrl)
                    VALUES (new.Id, new.Title, new.Author, COALESCE(new.Domain, ''),
                            new.Score, new.CreatedAt, new.CommentCount,
                            CASE WHEN new.Url IS NOT NULL AND new.Url != '' THEN 1 ELSE 0 END);
                END");

            // Populate with existing data
            migrationBuilder.Sql(@"
                INSERT INTO StoriesSearch(rowid, Title, Author, Domain, Score, CreatedAt, CommentCount, HasUrl)
                SELECT Id, Title, Author, COALESCE(Domain, ''), Score, CreatedAt, CommentCount,
                       CASE WHEN Url IS NOT NULL AND Url != '' THEN 1 ELSE 0 END
                FROM Stories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers first
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Stories_ai");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Stories_ad");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Stories_au");
            
            // Drop FTS table
            migrationBuilder.Sql("DROP TABLE IF EXISTS StoriesSearch");
        }
    }
}

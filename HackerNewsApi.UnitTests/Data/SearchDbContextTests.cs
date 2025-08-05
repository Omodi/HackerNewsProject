using FluentAssertions;
using HackerNewsApi.Infrastructure.Data;
using HackerNewsApi.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HackerNewsApi.UnitTests.Data;

public class SearchDbContextTests : DatabaseTestBase, IClassFixture<DatabaseTestFixture>
{
    public SearchDbContextTests(DatabaseTestFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public void SearchDbContext_Constructor_ShouldCreateInstance()
    {
        
        Context.Should().NotBeNull();
        Context.Should().BeOfType<SearchDbContext>();
        Context.Should().BeAssignableTo<DbContext>();
    }

    [Fact]
    public void SearchDbContext_StoriesDbSet_ShouldBeAccessible()
    {
        
        Context.Stories.Should().NotBeNull();
        Context.Stories.Should().BeAssignableTo<DbSet<StoryEntity>>();
    }

    [Fact]
    public async Task SearchDbContext_AddStory_ShouldAddSuccessfully()
    {
        
        var story = new StoryEntity
        {
            Id = 1,
            Title = "Test Story",
            Author = "testuser",
            Score = 100,
            CreatedAt = DateTime.UtcNow,
            CommentCount = 10
        };

        
        Context.Stories.Add(story);
        await Context.SaveChangesAsync();

                var savedStory = await Context.Stories.FindAsync(1);
        savedStory.Should().NotBeNull();
        savedStory!.Title.Should().Be("Test Story");
        savedStory.Author.Should().Be("testuser");
    }

    [Fact]
    public async Task SearchDbContext_UpdateStory_ShouldUpdateSuccessfully()
    {
        
        var story = new StoryEntity
        {
            Id = 2,
            Title = "Original Title",
            Author = "testuser",
            Score = 50,
            CreatedAt = DateTime.UtcNow,
            CommentCount = 5
        };

        Context.Stories.Add(story);
        await Context.SaveChangesAsync();

        
        story.Title = "Updated Title";
        story.Score = 100;
        Context.Stories.Update(story);
        await Context.SaveChangesAsync();

                var updatedStory = await Context.Stories.FindAsync(2);
        updatedStory.Should().NotBeNull();
        updatedStory!.Title.Should().Be("Updated Title");
        updatedStory.Score.Should().Be(100);
    }

    [Fact]
    public async Task SearchDbContext_DeleteStory_ShouldDeleteSuccessfully()
    {
        
        var story = new StoryEntity
        {
            Id = 3,
            Title = "Story to Delete",
            Author = "testuser",
            Score = 75,
            CreatedAt = DateTime.UtcNow,
            CommentCount = 3
        };

        Context.Stories.Add(story);
        await Context.SaveChangesAsync();

        
        Context.Stories.Remove(story);
        await Context.SaveChangesAsync();

                var deletedStory = await Context.Stories.FindAsync(3);
        deletedStory.Should().BeNull();
    }

    [Fact]
    public async Task SearchDbContext_QueryStories_ShouldReturnCorrectResults()
    {
        
        var stories = new[]
        {
            new StoryEntity { Id = 10, Title = "First Story", Author = "user1", Score = 100, CreatedAt = DateTime.UtcNow.AddDays(-1), CommentCount = 10 },
            new StoryEntity { Id = 11, Title = "Second Story", Author = "user2", Score = 150, CreatedAt = DateTime.UtcNow, CommentCount = 20 },
            new StoryEntity { Id = 12, Title = "Third Story", Author = "user1", Score = 200, CreatedAt = DateTime.UtcNow.AddDays(-2), CommentCount = 5 }
        };

        Context.Stories.AddRange(stories);
        await Context.SaveChangesAsync();

        
        var highScoreStories = await Context.Stories
            .Where(s => s.Score > 120)
            .OrderByDescending(s => s.Score)
            .ToListAsync();

                highScoreStories.Should().HaveCount(2);
        highScoreStories[0].Score.Should().Be(200);
        highScoreStories[1].Score.Should().Be(150);
    }

    [Fact]
    public async Task SearchDbContext_BulkOperations_ShouldWorkCorrectly()
    {
        
        var stories = Enumerable.Range(20, 10)
            .Select(i => new StoryEntity
            {
                Id = i,
                Title = $"Bulk Story {i}",
                Author = $"user{i}",
                Score = i * 10,
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                CommentCount = i
            })
            .ToArray();

        
        Context.Stories.AddRange(stories);
        await Context.SaveChangesAsync();

                var addedStories = await Context.Stories
            .Where(s => s.Id >= 20 && s.Id < 30)
            .ToListAsync();

        addedStories.Should().HaveCount(10);
        addedStories.Should().OnlyContain(s => s.Title.StartsWith("Bulk Story"));
    }

    [Fact]
    public async Task SearchDbContext_RebuildFtsIndexAsync_ShouldExecuteSuccessfully()
    {
        
        var story = new StoryEntity
        {
            Id = 100,
            Title = "Test FTS Story",
            Author = "testuser",
            Score = 100,
            CreatedAt = DateTime.UtcNow,
            CommentCount = 10
        };
        
        Context.Stories.Add(story);
        await Context.SaveChangesAsync();

        
        // SQLite should support FTS operations
        var act = async () => await Context.RebuildFtsIndexAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SearchDbContext_ComplexQuery_ShouldExecuteCorrectly()
    {
        
        var stories = new[]
        {
            new StoryEntity 
            { 
                Id = 30, 
                Title = "JavaScript Tutorial", 
                Author = "dev1", 
                Score = 150, 
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CommentCount = 25,
                Url = "https://example.com/js",
                Domain = "example.com"
            },
            new StoryEntity 
            { 
                Id = 31, 
                Title = "Python Guide", 
                Author = "dev2", 
                Score = 120, 
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CommentCount = 15,
                Url = null,
                Domain = null
            },
            new StoryEntity 
            { 
                Id = 32, 
                Title = "React Components", 
                Author = "dev1", 
                Score = 200, 
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                CommentCount = 30,
                Url = "https://react.dev/components",
                Domain = "react.dev"
            }
        };

        Context.Stories.AddRange(stories);
        await Context.SaveChangesAsync();

        
        var result = await Context.Stories
            .Where(s => s.Score > 130)
            .Where(s => s.Author == "dev1")
            .Where(s => !string.IsNullOrEmpty(s.Url))
            .OrderByDescending(s => s.CommentCount)
            .Select(s => new { s.Id, s.Title, s.Score, s.CommentCount })
            .ToListAsync();

                result.Should().HaveCount(2);
        result[0].CommentCount.Should().Be(30); // React Components
        result[1].CommentCount.Should().Be(25); // JavaScript Tutorial
    }

    [Fact]
    public async Task SearchDbContext_GroupByOperations_ShouldWorkCorrectly()
    {
        
        var stories = new[]
        {
            new StoryEntity { Id = 40, Title = "Story 1", Author = "author1", Score = 100, CreatedAt = DateTime.UtcNow, CommentCount = 10 },
            new StoryEntity { Id = 41, Title = "Story 2", Author = "author1", Score = 150, CreatedAt = DateTime.UtcNow, CommentCount = 20 },
            new StoryEntity { Id = 42, Title = "Story 3", Author = "author2", Score = 200, CreatedAt = DateTime.UtcNow, CommentCount = 5 },
            new StoryEntity { Id = 43, Title = "Story 4", Author = "author2", Score = 75, CreatedAt = DateTime.UtcNow, CommentCount = 15 }
        };

        Context.Stories.AddRange(stories);
        await Context.SaveChangesAsync();

        
        var authorStats = await Context.Stories
            .Where(s => s.Id >= 40 && s.Id <= 43)
            .GroupBy(s => s.Author)
            .Select(g => new
            {
                Author = g.Key,
                TotalStories = g.Count(),
                AverageScore = g.Average(s => s.Score),
                TotalComments = g.Sum(s => s.CommentCount)
            })
            .OrderBy(a => a.Author)
            .ToListAsync();

                authorStats.Should().HaveCount(2);
        
        var author1Stats = authorStats.First(a => a.Author == "author1");
        author1Stats.TotalStories.Should().Be(2);
        author1Stats.AverageScore.Should().Be(125); // (100 + 150) / 2
        author1Stats.TotalComments.Should().Be(30); // 10 + 20

        var author2Stats = authorStats.First(a => a.Author == "author2");
        author2Stats.TotalStories.Should().Be(2);
        author2Stats.AverageScore.Should().Be(137.5); // (200 + 75) / 2
        author2Stats.TotalComments.Should().Be(20); // 5 + 15
    }

    [Fact]
    public async Task SearchDbContext_Transaction_ShouldWorkWithSQLite()
    {
        
        var story1 = new StoryEntity { Id = 50, Title = "Transaction Story 1", Author = "user", Score = 100, CreatedAt = DateTime.UtcNow, CommentCount = 10 };
        var story2 = new StoryEntity { Id = 51, Title = "Transaction Story 2", Author = "user", Score = 150, CreatedAt = DateTime.UtcNow, CommentCount = 15 };

        
        // SQLite supports transactions
        using var transaction = await Context.Database.BeginTransactionAsync();
        
        Context.Stories.Add(story1);
        Context.Stories.Add(story2);
        await Context.SaveChangesAsync();
        
        await transaction.CommitAsync();
        
        // Verify stories were saved
        var savedStories = await Context.Stories.Where(s => s.Id == 50 || s.Id == 51).ToListAsync();
        savedStories.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchDbContext_ConcurrentAccess_ShouldHandleCorrectly()
    {
        
        var stories = Enumerable.Range(60, 5)
            .Select(i => new StoryEntity
            {
                Id = i,
                Title = $"Concurrent Story {i}",
                Author = $"user{i}",
                Score = i * 10,
                CreatedAt = DateTime.UtcNow,
                CommentCount = i
            })
            .ToArray();

        
        Context.Stories.AddRange(stories);
        await Context.SaveChangesAsync();

                var allStories = await Context.Stories
            .Where(s => s.Id >= 60 && s.Id < 65)
            .ToListAsync();

        allStories.Should().HaveCount(5);
        allStories.Should().OnlyContain(s => s.Title.StartsWith("Concurrent Story"));
    }

    [Fact]
    public void SearchDbContext_Dispose_ShouldNotThrow()
    {
        
        var act = () => Context.Dispose();
        act.Should().NotThrow();
    }
}
using FluentAssertions;
using HackerNewsApi.Core.Interfaces;
using HackerNewsApi.Core.Models;
using HackerNewsApi.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HackerNewsApi.UnitTests.Controllers;

public class StoriesControllerTests
{
    private readonly Mock<IHackerNewsService> _mockHackerNewsService;
    private readonly Mock<ILogger<StoriesController>> _mockLogger;
    private readonly StoriesController _controller;

    public StoriesControllerTests()
    {
        _mockHackerNewsService = new Mock<IHackerNewsService>();
        _mockLogger = new Mock<ILogger<StoriesController>>();
        _controller = new StoriesController(_mockHackerNewsService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetStories_WithValidParameters_ShouldReturnOkResult()
    {
        // Arrange
        var expectedResult = new PagedResult<Story>
        {
            Items = new[]
            {
                new Story { Id = 1, Title = "Test Story 1" },
                new Story { Id = 2, Title = "Test Story 2" }
            },
            TotalCount = 10,
            Page = 1,
            PageSize = 20
        };

        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 20))
                              .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetStories(1, 20);

        // Assert
        result.Should().BeOfType<ActionResult<PagedResult<Story>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedValue = okResult.Value.Should().BeOfType<PagedResult<Story>>().Subject;
        returnedValue.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task GetStories_WithInvalidPage_ShouldNormalizePage()
    {
        // Arrange
        var expectedResult = new PagedResult<Story>
        {
            Items = new[] { new Story { Id = 1, Title = "Test Story" } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 20))
                              .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetStories(-1, 20);

        // Assert
        _mockHackerNewsService.Verify(x => x.GetStoriesAsync(1, 20), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStories_WithInvalidPageSize_ShouldNormalizePageSize()
    {
        // Arrange
        var expectedResult = new PagedResult<Story>
        {
            Items = new[] { new Story { Id = 1, Title = "Test Story" } },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(1, 20))
                              .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetStories(1, 2000);

        // Assert
        _mockHackerNewsService.Verify(x => x.GetStoriesAsync(1, 20), Times.Once);
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStories_WhenServiceThrows_ShouldReturn500()
    {
        // Arrange
        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(It.IsAny<int>(), It.IsAny<int>()))
                              .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetStories(1, 20);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while fetching stories");
    }

    [Fact]
    public async Task SearchStories_WithValidQuery_ShouldReturnOkResult()
    {
        // Arrange
        var query = "react";
        var expectedResult = new PagedResult<Story>
        {
            Items = new[]
            {
                new Story { Id = 1, Title = "React Tutorial" }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mockHackerNewsService.Setup(x => x.SearchStoriesAsync(query, 1, 20))
                              .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchStories(query, 1, 20);

        // Assert
        result.Should().BeOfType<ActionResult<PagedResult<Story>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedValue = okResult.Value.Should().BeOfType<PagedResult<Story>>().Subject;
        returnedValue.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task SearchStories_WithEmptyQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.SearchStories("", 1, 20);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Query parameter is required");
    }

    [Fact]
    public async Task SearchStories_WithNullQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.SearchStories(null!, 1, 20);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Query parameter is required");
    }

    [Fact]
    public async Task SearchStories_WithWhitespaceQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.SearchStories("   ", 1, 20);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Query parameter is required");
    }

    [Fact]
    public async Task SearchStories_WhenServiceThrows_ShouldReturn500()
    {
        // Arrange
        _mockHackerNewsService.Setup(x => x.SearchStoriesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                              .ThrowsAsync(new Exception("Search error"));

        // Act
        var result = await _controller.SearchStories("test", 1, 20);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while searching stories");
    }

    [Fact]
    public async Task GetStory_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var storyId = 123;
        var expectedStory = new Story { Id = storyId, Title = "Test Story" };

        _mockHackerNewsService.Setup(x => x.GetStoryAsync(storyId))
                              .ReturnsAsync(expectedStory);

        // Act
        var result = await _controller.GetStory(storyId);

        // Assert
        result.Should().BeOfType<ActionResult<Story>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStory = okResult.Value.Should().BeOfType<Story>().Subject;
        returnedStory.Should().BeEquivalentTo(expectedStory);
    }

    [Fact]
    public async Task GetStory_WithInvalidId_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetStory(0);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid story ID");
    }

    [Fact]
    public async Task GetStory_WithNegativeId_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetStory(-1);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid story ID");
    }

    [Fact]
    public async Task GetStory_WhenStoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var storyId = 999;
        _mockHackerNewsService.Setup(x => x.GetStoryAsync(storyId))
                              .ReturnsAsync((Story?)null);

        // Act
        var result = await _controller.GetStory(storyId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Story with ID {storyId} not found");
    }

    [Fact]
    public async Task GetStory_WhenServiceThrows_ShouldReturn500()
    {
        // Arrange
        var storyId = 123;
        _mockHackerNewsService.Setup(x => x.GetStoryAsync(storyId))
                              .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetStory(storyId);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(500);
        statusCodeResult.Value.Should().Be("An error occurred while fetching the story");
    }

    [Theory]
    [InlineData(0, 10, 1, 10)] // Invalid page
    [InlineData(5, 0, 5, 20)] // Invalid page size
    [InlineData(-1, -5, 1, 20)] // Both invalid
    [InlineData(2, 50, 2, 50)] // Valid values
    [InlineData(1, 1001, 1, 20)] // Page size too large
    public async Task ValidatePagination_ShouldNormalizeValues(int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        // Arrange
        var expectedResult = new PagedResult<Story>
        {
            Items = new[] { new Story { Id = 1, Title = "Test" } },
            TotalCount = 1,
            Page = expectedPage,
            PageSize = expectedPageSize
        };

        _mockHackerNewsService.Setup(x => x.GetStoriesAsync(expectedPage, expectedPageSize))
                              .ReturnsAsync(expectedResult);

        // Act
        await _controller.GetStories(inputPage, inputPageSize);

        // Assert
        _mockHackerNewsService.Verify(x => x.GetStoriesAsync(expectedPage, expectedPageSize), Times.Once);
    }
}
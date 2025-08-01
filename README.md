# HackerNews API

A .NET 9.0 Web API that provides access to Hacker News stories with caching, pagination, and search functionality.

## Features

- **Latest Stories**: Get newest stories from Hacker News
- **Search**: Search stories by title with case-insensitive matching
- **Pagination**: Efficient pagination support
- **Caching**: Multi-tier caching strategy for optimal performance
- **Testing**: Comprehensive unit and integration tests

## API Endpoints

- `GET /api/stories` - Get paginated latest stories
- `GET /api/stories/search?query={query}` - Search stories
- `GET /api/stories/{id}` - Get specific story by ID
- `GET /health` - Health check endpoint

## Quick Start

```bash
# Clone the repository
git clone <repository-url>

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project HackerNewsApi.WebApi

# Run tests
dotnet test
```

## Technology Stack

- .NET 9.0
- ASP.NET Core Web API
- System.Text.Json
- IMemoryCache
- HttpClient with Polly
- xUnit + FluentAssertions + Moq

## Architecture

- **Clean Architecture** with Core, Infrastructure, and WebApi layers
- **Dependency Injection** for loose coupling
- **Async/await** patterns throughout
- **Retry policies** with Polly for resilient HTTP calls

## Deployment

The application is configured for deployment to Azure App Service with CI/CD via GitHub Actions.
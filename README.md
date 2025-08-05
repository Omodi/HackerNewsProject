# HackerNews Full-Stack Application

A modern full-stack application built with Angular frontend and .NET Core backend that displays HackerNews stories with pagination, search functionality, and real-time data caching.

## Architecture Overview

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Angular 20+   │    │  .NET Core 9.0  │    │  HackerNews API │
│    Frontend     │◄──►│   Backend API    │◄──►│   (External)    │
│                 │    │                 │    │                 │
│ • Standalone    │    │ • Clean Arch    │    │ • Firebase API  │
│ • Signals       │    │ • DI Container  │    │ • REST Endpoints│
│ • HttpClient    │    │ • IMemoryCache  │    │ • Story Data    │
│ • Routing       │    │ • Health Checks │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
           │                       │
           │            ┌─────────────────┐
           │            │  Azure App Svc  │
           │            │ (Production API) │
           │            └─────────────────┘
           │
    ┌─────────────────┐
    │ Google Custom   │
    │ Search Engine   │
    │ (Alternative)   │
    └─────────────────┘
```

## Features Implemented

### Core Requirements
- Angular Frontend: Modern Angular 20.1.4 with standalone components
- .NET Core Backend: Clean Architecture with .NET 9.0
- HackerNews API Integration: Fetches newest stories with caching
- Pagination: Working pagination with page navigation
- Search Functionality: Search stories by title
- Caching: In-memory caching for performance optimization
- Dependency Injection: Proper DI throughout the application
- Automated Testing: 55+ unit and integration tests
- Azure Deployment: Production API deployed to Azure

### Advanced Features
- Real-time Data: Live HackerNews data with smart caching
- Responsive Design: Mobile-first UI with HackerNews styling
- Error Handling: Comprehensive error handling and loading states
- Health Monitoring: API health checks and logging
- CI/CD Pipeline: GitHub Actions for automated testing and deployment
- Alternative Search: Google Custom Search Engine integration
- Clean Code: SOLID principles and best practices

## Frontend (Angular)

### Technology Stack
- Angular 20.1.4 with standalone components
- TypeScript for type safety
- RxJS for reactive programming
- Angular Signals for state management
- SCSS for styling
- Jasmine/Karma for testing

### Key Components
- StoryList: Main component displaying paginated stories
- HackerNewsService: Service for API communication
- GoogleSearchTest: Alternative search implementation

### Features
```typescript
// Modern Angular with Signals
stories = signal<Story[]>([]);
loading = signal<boolean>(false);
currentPage = signal<number>(1);

// Reactive data loading
this.hackerNewsService.getStories(page, 20).subscribe({
  next: (result) => this.stories.set(result.items)
});
```

## Backend (.NET Core)

### Architecture
```
HackerNewsApi/
├── Core/                 # Domain models and interfaces
│   ├── Models/           # Story, PagedResult entities
│   └── Interfaces/       # Service contracts
├── Infrastructure/       # External concerns
│   └── Services/         # API clients, caching
├── WebApi/              # Controllers and startup
│   ├── Controllers/      # REST endpoints
│   └── Program.cs        # DI configuration
└── Tests/               # Unit and integration tests
    ├── UnitTests/        # Service and logic tests
    └── IntegrationTests/ # API endpoint tests
```

### API Endpoints
```http
GET /api/stories?page=1&pageSize=20
GET /api/stories/search?query=python&page=1&pageSize=20
GET /api/stories/{id}
GET /api/health
```

### Caching Strategy
- **Story IDs**: 5-minute cache for latest story list
- **Individual Stories**: 30-minute cache for story details
- **Search Results**: 2-minute cache for search queries

## Testing

### Backend Testing (55+ Tests)
```bash
# Run all tests
dotnet test

# Test Coverage
- Unit Tests: Services, caching, data transformation
- Integration Tests: API endpoints, HTTP responses
- Health Check Tests: Monitoring endpoints
```

### Test Statistics
- 41 Unit Tests: Service logic and business rules
- 14 Integration Tests: API endpoints and HTTP flows
- 100% Pass Rate: All tests passing consistently

## Deployment

### Production Environment
- **Backend API**: Deployed to Azure App Service
- **Production URL**: `https://hackernewsapigreen-djgchbfwf6ead2hf.canadacentral-01.azurewebsites.net`
- **CI/CD**: GitHub Actions for automated deployment
- **Monitoring**: Application Insights and health checks

### Local Development
```bash
# Backend
cd HackerNewsApi.WebApi
dotnet run
# API available at: https://localhost:5001

# Frontend  
cd hackernews-frontend
npm install
npm start
# App available at: http://localhost:4200
```

## User Interface

### Design Principles
- HackerNews Aesthetic: Orange (#ff6600) color scheme
- Responsive Design: Mobile-first approach
- Loading States: Smooth loading indicators
- Error Handling: User-friendly error messages

### Key UI Features
- Story ranking and metadata display
- Pagination controls (Previous/Next)
- Search functionality with real-time feedback
- External link handling
- Time-ago formatting
- Domain extraction from URLs

## Search Implementation

### Option 1: Backend API Search
- Searches cached story titles
- Limited to ~500 most recent stories
- Fast response with consistent styling
- Integrated pagination

### Option 2: Google Custom Search Engine
- Comprehensive HackerNews content search
- Powered by Google's search algorithms
- Real-time results from entire HackerNews archive
- Custom styling to match application design

## Performance Optimizations

### Caching Strategy
```csharp
// Intelligent caching with different TTLs
private static readonly TimeSpan StoryIdsCacheExpiry = TimeSpan.FromMinutes(5);
private static readonly TimeSpan StoryCacheExpiry = TimeSpan.FromMinutes(30);
private static readonly TimeSpan SearchCacheExpiry = TimeSpan.FromMinutes(2);
```

### Frontend Optimizations
- Angular Signals for reactive updates
- Lazy loading and code splitting
- Optimized HTTP requests with RxJS
- Efficient pagination state management

## Development Setup

### Prerequisites
- Node.js 18+ and npm
- .NET 9.0 SDK
- Git

### Quick Start
```bash
# Clone repository
git clone <repository-url>
cd HackerNews

# Setup backend
cd HackerNewsApi.WebApi
dotnet restore
dotnet run

# Setup frontend (new terminal)
cd hackernews-frontend
npm install
npm start

# Run tests
dotnet test                    # Backend tests
ng test                        # Frontend tests (when implemented)
```

## CI/CD Pipeline

### GitHub Actions Workflows
- **Continuous Integration**: Automated testing on every commit
- **Continuous Deployment**: Automatic Azure deployment on main branch
- **Test Coverage**: Ensures all tests pass before deployment

## Technical Highlights

### Clean Architecture
- **Separation of Concerns**: Clear layers and responsibilities
- **Dependency Injection**: Proper DI throughout application
- **SOLID Principles**: Maintainable and extensible code
- **Interface Segregation**: Well-defined service contracts

### Modern Development Practices
- **Type Safety**: TypeScript frontend, strongly-typed C# backend
- **Reactive Programming**: RxJS observables and Angular signals
- **Error Boundaries**: Comprehensive error handling
- **Performance Monitoring**: Health checks and logging

## Project Status

### Completed Features
- Full-stack application with Angular + .NET Core
- Working pagination and search functionality
- Production-ready backend deployed to Azure
- Comprehensive testing suite (55+ tests)
- CI/CD pipeline with GitHub Actions
- Professional UI with HackerNews styling
- Multiple search implementation options

### Demonstration Ready
This application successfully demonstrates:
- Modern full-stack development skills
- Clean architecture and best practices
- Testing and deployment capabilities
- Performance optimization techniques
- Professional UI/UX implementation

---

Built using Angular, .NET Core, and modern development practices
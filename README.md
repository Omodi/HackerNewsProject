# HackerNews Full-Stack Application

A modern full-stack application that displays HackerNews stories with search functionality and real-time data caching.

## Tech Stack
- **Frontend**: Angular 20+ with standalone components
- **Backend**: .NET Core 9.0 with clean architecture
- **Database**: SQLite with full-text search (FTS5)
- **Deployment**: Azure Static Web Apps + Azure App Service

## Features
- Browse latest HackerNews stories with pagination
- Advanced search functionality with filters
- Real-time data caching for performance
- Responsive design optimized for all devices
- Comprehensive testing suite

## Quick Start

### Backend
```bash
cd HackerNewsApi.WebApi
dotnet restore
dotnet run
```

### Frontend
```bash
cd hackernews-frontend
npm install
npm start
```

### Testing
```bash
dotnet test  # Run all backend tests
```

## Live Demo
- **Frontend**: Deployed via Azure Static Web Apps
- **Backend API**: https://hackernewsapigreen-djgchbfwf6ead2hf.canadacentral-01.azurewebsites.net

## API Endpoints
- `GET /api/stories` - Get paginated stories
- `GET /api/search` - Search stories with filters
- `GET /api/health` - Health check

Built with Angular, .NET Core, and modern development practices.
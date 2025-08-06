# HackerNews Full-Stack Application

A modern full-stack application that provides a clean interface for browsing HackerNews stories with search functionality.

## Prerequisites

- **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 18+** - [Download here](https://nodejs.org/)
- **npm** (comes with Node.js)

## Installation & Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd HackerNews
```

### 2. Backend Setup (ASP.NET Core API)
```bash
# Restore dependencies for all projects
dotnet restore

# Build the entire solution
dotnet build

# Run the API
# Option 1: HTTP mode (no certificate required)
dotnet run --project HackerNewsApi.WebApi --launch-profile http
# API will be available at http://localhost:5076

# Option 2: HTTPS mode (requires trusted certificate)
dotnet dev-certs https --trust
dotnet run --project HackerNewsApi.WebApi --launch-profile https
# API will be available at https://localhost:7070
```

### 3. Frontend Setup (Angular)
```bash
# Navigate to the frontend project
cd hackernews-frontend

# Install dependencies
npm install

# Build the project
npm run build

# Start the development server (will be available at http://localhost:4200)
npm start
```

## Development Commands

### Backend
```bash
# Run all tests
dotnet test

```

### Frontend
```bash
# Run tests
npm test

# Build for production
npm run build

```

## Live Demo
- https://icy-tree-06e261e0f.2.azurestaticapps.net/
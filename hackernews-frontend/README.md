# HackerNews Frontend

Angular 20.1.4 frontend for the HackerNews application.

## Features

- View newest HackerNews stories with pagination
- Search stories by title
- Responsive design with HackerNews styling
- Angular Signals for reactive state management
- Comprehensive unit tests

## Development Setup

Start the development server:

```bash
npm install
ng serve
```

Navigate to `http://localhost:4200/`. The app will automatically reload when you change any source files.

## Building

Build the project for production:

```bash
ng build
```

The build artifacts will be stored in the `dist/` directory.

## Testing

Run unit tests:

```bash
ng test
```

All 33 tests should pass.

## Project Structure

```
src/app/
├── components/
│   └── story-list/          # Main story display component
├── services/
│   └── hackernews.service.ts # API communication service
└── models/
    └── story.ts             # TypeScript interfaces
```

## Backend Integration

This frontend connects to the .NET Core backend API:
- Local: `http://localhost:5000`
- Production: `https://hackernewsapigreen-djgchbfwf6ead2hf.canadacentral-01.azurewebsites.net`

The backend provides REST endpoints for stories, search, and pagination.

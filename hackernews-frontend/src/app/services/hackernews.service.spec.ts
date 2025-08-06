import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';
import { HackerNewsService } from './hackernews.service';
import { Story, PagedStoryResult, SearchSortOrder } from '../models/story.model';
import { environment } from '../../environments/environment';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('HackerNewsService', () => {
  let service: HackerNewsService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [],
      providers: [HackerNewsService, provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(HackerNewsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getStories', () => {
    it('should fetch stories with default parameters', () => {
      const mockApiResponse: PagedStoryResult = {
        items: [
          {
            id: 1,
            title: 'Test Story',
            by: 'testuser',
            time: 1672531200, // Unix timestamp for 2023-01-01T00:00:00Z
            createdAt: '2023-01-01T00:00:00Z',
            score: 100,
            url: 'https://example.com',
            type: 'story',
            hasUrl: true,
            kids: [101, 102],
            descendants: 10,
            commentCount: 10,
            hackerNewsUrl: 'https://news.ycombinator.com/item?id=1'
          }
        ],
        page: 1,
        pageSize: 20
      };

      service.getStories().subscribe(result => {
        expect(result.items.length).toBe(1);
        expect(result.items[0].title).toBe('Test Story');
        expect(result.page).toBe(1);
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      expect(req.request.method).toBe('GET');
      req.flush(mockApiResponse);
    });

    it('should fetch stories with custom page and size', () => {
      const mockResponse: PagedStoryResult = {
        items: [],
        page: 2,
        pageSize: 10
      };

      service.getStories(2, 10).subscribe(result => {
        expect(result.page).toBe(2);
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=2&pageSize=10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should return API response with correct data types', () => {
      const mockApiResponse = {
        items: [
          {
            id: 1,
            title: 'Test Story',
            by: 'testuser',
            time: 1672531200,
            createdAt: '2023-01-01T00:00:00Z',
            score: 100,
            url: 'https://example.com',
            type: 'story',
            hasUrl: true,
            kids: [201, 202],
            descendants: 5,
            commentCount: 5,
            hackerNewsUrl: 'https://news.ycombinator.com/item?id=1'
          }
        ],
        page: 1
      };

      service.getStories().subscribe(result => {
        const story = result.items[0];
        expect(typeof story.time).toBe('number');
        expect(typeof story.createdAt).toBe('string');
        expect(story.time).toBe(1672531200);
        expect(story.createdAt).toBe('2023-01-01T00:00:00Z');
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      req.flush(mockApiResponse);
    });
  });


  describe('normalizeStory (private method testing through public methods)', () => {
    it('should handle stories without URLs', () => {
      const mockApiResponse = {
        items: [
          {
            id: 3,
            title: 'No URL Story',
            by: 'nourl',
            time: 1672531200,
            createdAt: '2023-01-01T00:00:00Z',
            score: 75,
            type: 'story',
            hasUrl: false,
            kids: [401, 402, 403],
            descendants: 8,
            commentCount: 8,
            hackerNewsUrl: 'https://news.ycombinator.com/item?id=3'
          }
        ],
        page: 1
      };

      service.getStories().subscribe(result => {
        const story = result.items[0];
        expect(story.hasUrl).toBe(false);
        expect(story.url).toBeUndefined();
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      req.flush(mockApiResponse);
    });
  });

  describe('error handling', () => {
    it('should handle HTTP errors gracefully', () => {
      service.getStories().subscribe({
        next: () => fail('Expected an error'),
        error: (error) => {
          expect(error).toBeTruthy();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      req.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });
    });

    it('should handle network errors', () => {
      const searchQuery = {
        query: 'test',
        page: 1,
        pageSize: 20,
        sortBy: SearchSortOrder.Relevance
      };

      service.searchStories(searchQuery).subscribe({
        next: () => fail('Expected an error'),
        error: (error) => {
          expect(error).toBeTruthy();
        }
      });

      const req = httpMock.expectOne(`${baseUrl}/search?q=test&page=1&pageSize=20&sortBy=Relevance`);
      req.error(new ErrorEvent('Network error'));
    });
  });

  describe('searchStories', () => {
    it('should perform search with basic query', () => {
      const searchQuery = {
        query: 'test',
        page: 1,
        pageSize: 20,
        sortBy: SearchSortOrder.Relevance
      };

      const mockResponse: PagedStoryResult = {
        items: [
          {
            id: 1,
            title: 'Enhanced Search Result',
            by: 'testuser',
            time: Date.now() / 1000, // Unix timestamp
            createdAt: new Date().toISOString(),
            score: 100,
            type: 'story',
            hasUrl: true,
            kids: [101],
            descendants: 1,
            commentCount: 1,
            hackerNewsUrl: 'https://news.ycombinator.com/item?id=1'
          }
        ],
        page: 1,
        pageSize: 20
      };

      service.searchStories(searchQuery).subscribe(result => {
        expect(result.items.length).toBe(1);
        expect(result.items[0].title).toBe('Enhanced Search Result');
      });

      const req = httpMock.expectOne(`${baseUrl}/search?q=test&page=1&pageSize=20&sortBy=Relevance`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should perform search with filters', () => {
      const searchQuery = {
        query: 'javascript',
        page: 1,
        pageSize: 10,
        sortBy: SearchSortOrder.Score,
        filters: {
          minScore: 50,
          author: 'testuser',
          hasUrl: true
        }
      };

      const mockResponse: PagedStoryResult = {
        items: [],
        page: 1,
        pageSize: 10
      };

      service.searchStories(searchQuery).subscribe(result => {
        expect(result.page).toBe(1);
      });

      const req = httpMock.expectOne(`${baseUrl}/search?q=javascript&page=1&pageSize=10&sortBy=Score&minScore=50&author=testuser&hasUrl=true`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('getSearchSuggestions', () => {
    it('should get search suggestions', () => {
      const mockSuggestions = ['javascript', 'java', 'react'];

      service.getSearchSuggestions('java').subscribe(suggestions => {
        expect(suggestions).toEqual(mockSuggestions);
        expect(suggestions.length).toBe(3);
      });

      const req = httpMock.expectOne(`${baseUrl}/search/suggestions?q=java&limit=10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSuggestions);
    });

    it('should get search suggestions with custom limit', () => {
      const mockSuggestions = ['python', 'pytorch'];

      service.getSearchSuggestions('python', 5).subscribe(suggestions => {
        expect(suggestions).toEqual(mockSuggestions);
      });

      const req = httpMock.expectOne(`${baseUrl}/search/suggestions?q=python&limit=5`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSuggestions);
    });
  });

  describe('searchStories - advanced filter scenarios', () => {
    it('should handle search with date filters', () => {
      const fromDate = '2023-01-01T00:00:00.000Z';
      const toDate = '2023-12-31T23:59:59.999Z';
      const searchQuery = {
        query: 'javascript',
        page: 1,
        pageSize: 20,
        sortBy: SearchSortOrder.Recent,
        filters: {
          fromDate,
          toDate,
          minScore: 50
        }
      };

      const mockResponse: PagedStoryResult = {
        items: [],
        page: 1,
        pageSize: 20
      };

      service.searchStories(searchQuery).subscribe(result => {
        expect(result.page).toBe(1);
      });

      const req = httpMock.expectOne((request) => {
        return request.url.includes('/search') &&
               request.params.get('fromDate') === fromDate &&
               request.params.get('toDate') === toDate &&
               request.params.get('minScore') === '50';
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle search with all filter types', () => {
      const searchQuery = {
        query: 'react',
        page: 2,
        pageSize: 10,
        sortBy: SearchSortOrder.Score,
        filters: {
          minScore: 10,
          maxScore: 100,
          author: 'testuser',
          domain: 'github.com',
          hasUrl: true
        }
      };

      const mockResponse: PagedStoryResult = {
        items: [],
        page: 2,
        pageSize: 10
      };

      service.searchStories(searchQuery).subscribe(result => {
        expect(result.page).toBe(2);
      });

      const req = httpMock.expectOne((request) => {
        return request.url.includes('/search') &&
               request.params.get('maxScore') === '100' &&
               request.params.get('author') === 'testuser' &&
               request.params.get('domain') === 'github.com' &&
               request.params.get('hasUrl') === 'true';
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle search with minimal parameters and fallback values', () => {
      const searchQuery = {
        query: '', // Empty query to test fallback
        // Missing optional parameters to test defaults
      };

      const mockResponse: PagedStoryResult = {
        items: [],
        page: 1,
        pageSize: 20
      };

      service.searchStories(searchQuery).subscribe(result => {
        expect(result.page).toBe(1);
      });

      const req = httpMock.expectOne((request) => {
        return request.url.includes('/search') &&
               request.params.get('q') === '' &&
               request.params.get('page') === '1' &&
               request.params.get('pageSize') === '20' &&
               request.params.get('sortBy') === SearchSortOrder.Relevance;
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle search without filters', () => {
      const searchQuery = {
        query: 'test',
        page: 1,
        pageSize: 20,
        sortBy: SearchSortOrder.Relevance
        // No filters property
      };

      const mockResponse: PagedStoryResult = {
        items: [],
        page: 1,
        pageSize: 20
      };

      service.searchStories(searchQuery).subscribe(result => {
        expect(result.page).toBe(1);
      });

      const req = httpMock.expectOne(`${baseUrl}/search?q=test&page=1&pageSize=20&sortBy=Relevance`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('normalizeStory - edge cases and fallbacks', () => {
    it('should handle story with missing commentCount and descendants', () => {
      const apiStory = {
        id: 1,
        title: 'Test Story',
        by: 'testuser',
        time: 1672531200,
        createdAt: '2023-01-01T00:00:00Z',
        score: 100,
        type: 'story',
        hasUrl: false
      };

      service.getStories().subscribe(result => {
        const story = result.items[0];
        expect(story.commentCount).toBe(0);
        expect(story.hackerNewsUrl).toBe('https://news.ycombinator.com/item?id=1');
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      req.flush({
        items: [apiStory],
        page: 1,
        pageSize: 20
      });
    });

    it('should handle story with existing commentCount', () => {
      const apiStory = {
        id: 2,
        title: 'Test Story with Comments',
        by: 'testuser',
        time: 1672531200,
        createdAt: '2023-01-01T00:00:00Z',
        score: 150,
        type: 'story',
        hasUrl: true,
        url: 'https://example.com',
        commentCount: 25,
        hackerNewsUrl: 'https://news.ycombinator.com/item?id=2'
      };

      service.getStories().subscribe(result => {
        const story = result.items[0];
        expect(story.commentCount).toBe(25);
        expect(story.hackerNewsUrl).toBe('https://news.ycombinator.com/item?id=2');
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      req.flush({
        items: [apiStory],
        page: 1,
        pageSize: 20
      });
    });

    it('should fallback to descendants when commentCount is missing', () => {
      const apiStory = {
        id: 3,
        title: 'Test Story with Descendants',
        by: 'testuser',
        time: 1672531200,
        createdAt: '2023-01-01T00:00:00Z',
        score: 75,
        type: 'story',
        hasUrl: false,
        descendants: 15
      };

      service.getStories().subscribe(result => {
        const story = result.items[0];
        expect(story.commentCount).toBe(15);
      });

      const req = httpMock.expectOne(`${baseUrl}/stories?page=1&pageSize=20`);
      req.flush({
        items: [apiStory],
        page: 1,
        pageSize: 20
      });
    });
  });
});
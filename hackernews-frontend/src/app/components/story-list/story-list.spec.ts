import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError, NEVER } from 'rxjs';
import { StoryList } from './story-list';
import { HackerNewsService } from '../../services/hackernews.service';
import { Story, PagedStoryResult } from '../../models/story.model';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Component } from '@angular/core';

@Component({
  selector: 'mock-router-link',
  template: '<ng-content></ng-content>'
})
class MockRouterLinkComponent {
  routerLink: any;
}

describe('StoryList', () => {
  let component: StoryList;
  let fixture: ComponentFixture<StoryList>;
  let mockHackerNewsService: jasmine.SpyObj<HackerNewsService>;

  const mockStories: Story[] = [
    {
      id: 1,
      title: 'Test Story 1',
      by: 'user1',
      time: new Date('2023-01-01T10:00:00Z'),
      createdAt: new Date('2023-01-01T10:00:00Z'),
      score: 100,
      url: 'https://example.com/story1',
      type: 'story',
      hasUrl: true,
      kids: [1001, 1002],
      descendants: 5,
      commentCount: 5,
      hackerNewsUrl: 'https://news.ycombinator.com/item?id=1'
    },
    {
      id: 2,
      title: 'Test Story 2',
      by: 'user2',
      time: new Date('2023-01-01T11:00:00Z'),
      createdAt: new Date('2023-01-01T11:00:00Z'),
      score: 50,
      type: 'story',
      hasUrl: false,
      kids: [2001],
      descendants: 3,
      commentCount: 3,
      hackerNewsUrl: 'https://news.ycombinator.com/item?id=2'
    }
  ];

  const mockPagedResult: PagedStoryResult = {
    items: mockStories,
    totalCount: 2,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('HackerNewsService', [
      'getStories',
      'searchStories',
      'getSearchSuggestions'
    ]);

    await TestBed.configureTestingModule({
      imports: [StoryList, FormsModule],
      providers: [
        { provide: HackerNewsService, useValue: spy }
      ]
    })
    .overrideComponent(StoryList, {
      remove: { imports: [RouterLink] },
      add: { imports: [MockRouterLinkComponent] }
    })
    .compileComponents();

    fixture = TestBed.createComponent(StoryList);
    component = fixture.componentInstance;
    mockHackerNewsService = TestBed.inject(HackerNewsService) as jasmine.SpyObj<HackerNewsService>;
  });

  beforeEach(() => {
    mockHackerNewsService.getStories.and.returnValue(of(mockPagedResult));
    mockHackerNewsService.searchStories.and.returnValue(of(mockPagedResult));
    mockHackerNewsService.getSearchSuggestions.and.returnValue(of(['suggestion1', 'suggestion2']));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load stories on initialization', () => {
      component.ngOnInit();
      
      expect(mockHackerNewsService.getStories).toHaveBeenCalledWith(1, 20);
      expect(component.stories()).toEqual(mockStories);
      expect(component.loading()).toBe(false);
      expect(component.error()).toBeNull();
    });
  });

  describe('loadStories', () => {
    it('should load stories successfully', () => {
      component.loadStories(2);

      expect(component.loading()).toBe(false);
      expect(component.currentPage()).toBe(2);
      expect(component.stories()).toEqual(mockStories);
      expect(component.error()).toBeNull();
      expect(mockHackerNewsService.getStories).toHaveBeenCalledWith(2, 20);
    });

    it('should set loading state during API call', () => {
      expect(component.loading()).toBe(false);
      
      component.loadStories(1);
      
      expect(component.loading()).toBe(false);
    });

    it('should handle API errors', () => {
      const errorMessage = 'API Error';
      mockHackerNewsService.getStories.and.returnValue(throwError(() => new Error(errorMessage)));

      component.loadStories(1);

      expect(component.loading()).toBe(false);
      expect(component.error()).toBe('Failed to load stories. Please try again.');
      expect(component.stories()).toEqual([]);
    });

    it('should search stories when search query is set', () => {
      component.searchQuery.set('test query');
      component.isSearchMode.set(true);

      component.loadStories(1);

      expect(mockHackerNewsService.searchStories).toHaveBeenCalled();
      expect(mockHackerNewsService.getStories).not.toHaveBeenCalled();
    });
  });

  describe('pagination', () => {
    beforeEach(() => {
      component.ngOnInit();
    });

    it('should go to next page', () => {
      component.currentPage.set(1);
      
      component.goToNextPage();

      expect(component.currentPage()).toBe(2);
      expect(mockHackerNewsService.getStories).toHaveBeenCalledWith(2, 20);
    });

    it('should go to previous page', () => {
      component.currentPage.set(3);
      
      component.goToPreviousPage();

      expect(component.currentPage()).toBe(2);
      expect(mockHackerNewsService.getStories).toHaveBeenCalledWith(2, 20);
    });

    it('should not go to previous page when on page 1', () => {
      component.currentPage.set(1);
      const initialCallCount = mockHackerNewsService.getStories.calls.count();
      
      component.goToPreviousPage();

      expect(component.currentPage()).toBe(1);
      expect(mockHackerNewsService.getStories.calls.count()).toBe(initialCallCount);
    });
  });

  describe('search functionality', () => {
    it('should perform search with query', () => {
      component.onSearch('python');

      expect(component.searchQuery()).toBe('python');
      expect(component.isSearchMode()).toBe(true);
      expect(component.currentPage()).toBe(1);
      expect(mockHackerNewsService.searchStories).toHaveBeenCalled();
    });

    it('should trim search query', () => {
      component.onSearch('  javascript  ');

      expect(component.searchQuery()).toBe('javascript');
      expect(component.isSearchMode()).toBe(true);
    });

    it('should handle empty search query', () => {
      component.onSearch('   ');

      expect(component.searchQuery()).toBe('');
      expect(component.isSearchMode()).toBe(false);
    });

    it('should clear search', () => {
      component.searchQuery.set('test');
      component.isSearchMode.set(true);
      component.currentPage.set(3);

      component.clearSearch();

      expect(component.searchQuery()).toBe('');
      expect(component.isSearchMode()).toBe(false);
      expect(component.currentPage()).toBe(1);
      expect(mockHackerNewsService.getStories).toHaveBeenCalledWith(1, 20);
    });
  });

  describe('utility methods', () => {
    it('should format time ago correctly', () => {
      const now = new Date();
      
      const thirtyMinutesAgo = new Date(now.getTime() - 30 * 60 * 1000);
      expect(component.formatTimeAgo(thirtyMinutesAgo)).toBe('30m ago');

      const oneHourAgo = new Date(now.getTime() - 60 * 60 * 1000);
      expect(component.formatTimeAgo(oneHourAgo)).toBe('1h ago');

      const threeHoursAgo = new Date(now.getTime() - 3 * 60 * 60 * 1000);
      expect(component.formatTimeAgo(threeHoursAgo)).toBe('3h ago');

      const oneDayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
      expect(component.formatTimeAgo(oneDayAgo)).toBe('1d ago');

      const threeDaysAgo = new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000);
      expect(component.formatTimeAgo(threeDaysAgo)).toBe('3d ago');
    });

    it('should extract domain from URL', () => {
      expect(component.getDomainFromUrl('https://www.example.com/path')).toBe('example.com');
      expect(component.getDomainFromUrl('http://github.com/user/repo')).toBe('github.com');
      expect(component.getDomainFromUrl('https://subdomain.example.org')).toBe('subdomain.example.org');
    });

    it('should handle invalid URLs', () => {
      expect(component.getDomainFromUrl('invalid-url')).toBe('');
      expect(component.getDomainFromUrl('')).toBe('');
      expect(component.getDomainFromUrl(undefined)).toBe('');
    });
  });

  describe('signal state management', () => {
    it('should initialize signals with correct default values', () => {
      expect(component.stories()).toEqual([]);
      expect(component.currentPage()).toBe(1);
      expect(component.loading()).toBe(false);
      expect(component.error()).toBeNull();
      expect(component.searchQuery()).toBe('');
      expect(component.isSearchMode()).toBe(false);
    });

    it('should update signals correctly during operations', () => {
      component.loading.set(true);
      expect(component.loading()).toBe(true);

      component.error.set('Test error');
      expect(component.error()).toBe('Test error');

      component.searchQuery.set('test');
      component.isSearchMode.set(true);
      expect(component.searchQuery()).toBe('test');
      expect(component.isSearchMode()).toBe(true);
    });
  });

  describe('search suggestions', () => {
    it('should trigger search suggestions on input', () => {
      const event = { target: { value: 'javascript' } } as any;
      
      component.onSearchInput(event);
      
      expect(component.searchQuery()).toBe('javascript');
    });

    it('should not trigger suggestions for short queries', () => {
      const event = { target: { value: 'j' } } as any;
      
      component.onSearchInput(event);
      
      expect(component.searchQuery()).toBe('j');
      expect(component.suggestions()).toEqual([]);
    });

    it('should clear suggestions for empty query', () => {
      component.suggestions.set(['test1', 'test2']);
      const event = { target: { value: '' } } as any;
      
      component.onSearchInput(event);
      
      expect(component.suggestions()).toEqual([]);
    });

    it('should handle suggestion selection', () => {
      const mockEvent = { option: { value: 'selected suggestion' } } as any;
      spyOn(component, 'onSearch');
      
      component.onSuggestionSelected(mockEvent);
      
      expect(component.searchQuery()).toBe('selected suggestion');
      expect(component.onSearch).toHaveBeenCalledWith('selected suggestion');
    });

    it('should display suggestion correctly', () => {
      expect(component.displaySuggestion('test suggestion')).toBe('test suggestion');
      expect(component.displaySuggestion('')).toBe('');
    });
  });

  describe('filters functionality', () => {
    it('should toggle filters expanded state', () => {
      expect(component.filtersExpanded()).toBe(false);
      
      component.toggleFilters();
      
      expect(component.filtersExpanded()).toBe(true);
      
      component.toggleFilters();
      
      expect(component.filtersExpanded()).toBe(false);
    });

    it('should clear all filters', () => {
      component.filtersForm.patchValue({
        fromDate: new Date(),
        author: 'test',
        minScore: 50
      });
      spyOn(component, 'onFiltersChanged');
      
      component.clearAllFilters();
      
      expect(component.filtersForm.get('fromDate')?.value).toBeNull();
      expect(component.filtersForm.get('author')?.value).toBeNull();
      expect(component.filtersForm.get('minScore')?.value).toBeNull();
      expect(component.onFiltersChanged).toHaveBeenCalled();
    });

    it('should handle filters changed', () => {
      component.searchQuery.set('test');
      spyOn(component, 'loadStories');
      
      component.onFiltersChanged();
      
      expect(component.isSearchMode()).toBe(true);
      expect(component.currentPage()).toBe(1);
      expect(component.loadStories).toHaveBeenCalledWith(1);
    });

    it('should count active filters correctly', () => {
      expect(component.getActiveFiltersCount()).toBe(0);
      
      component.filtersForm.patchValue({
        fromDate: new Date(),
        author: 'test',
        minScore: 50
      });
      
      expect(component.getActiveFiltersCount()).toBe(3);
    });

    it('should detect active filters with all filter types', () => {
      component.filtersForm.patchValue({
        fromDate: new Date(),
        toDate: new Date(),
        minScore: 10,
        maxScore: 100,
        author: 'testuser',
        domain: 'example.com',
        hasUrl: true
      });
      
      expect(component.getActiveFiltersCount()).toBe(7);
    });

    it('should build filters object correctly', () => {
      const fromDate = new Date('2023-01-01');
      const toDate = new Date('2023-12-31');
      
      component.filtersForm.patchValue({
        fromDate,
        toDate,
        minScore: 10,
        maxScore: 100,
        author: 'testuser',
        domain: 'example.com',
        hasUrl: true
      });
      
      // Call private method through loadStories with search mode
      component.searchQuery.set('test');
      component.loadStories(1);
      
      expect(mockHackerNewsService.searchStories).toHaveBeenCalledWith(
        jasmine.objectContaining({
          filters: jasmine.objectContaining({
            minScore: 10,
            author: 'testuser',
            hasUrl: true
          })
        })
      );
    });

    it('should handle empty and whitespace-only filter values', () => {
      component.filtersForm.patchValue({
        author: '  ',
        domain: '',
        minScore: '',
        maxScore: null
      });
      
      expect(component.getActiveFiltersCount()).toBe(0);
    });
  });

  describe('formatTimeAgo edge cases', () => {
    it('should handle "just now" case', () => {
      const now = new Date();
      const justNow = new Date(now.getTime() - 30000); // 30 seconds ago
      
      expect(component.formatTimeAgo(justNow)).toBe('just now');
    });

    it('should handle invalid dates', () => {
      const invalidDate = new Date('invalid');
      
      expect(component.formatTimeAgo(invalidDate)).toBe('unknown');
    });

    it('should handle string dates', () => {
      const dateString = '2023-01-01T00:00:00Z';
      const result = component.formatTimeAgo(dateString);
      
      expect(result).toMatch(/\d+[dmy] ago/);
    });

    it('should handle months and years', () => {
      const now = new Date();
      
      const sixMonthsAgo = new Date(now.getTime() - 6 * 30 * 24 * 60 * 60 * 1000);
      expect(component.formatTimeAgo(sixMonthsAgo)).toBe('6mo ago');
      
      const twoYearsAgo = new Date(now.getTime() - 2 * 365 * 24 * 60 * 60 * 1000);
      expect(component.formatTimeAgo(twoYearsAgo)).toBe('2y ago');
    });
  });

  describe('error handling in loadStories', () => {
    it('should handle search stories error', () => {
      component.searchQuery.set('test query');
      component.isSearchMode.set(true);
      mockHackerNewsService.searchStories.and.returnValue(throwError(() => new Error('Search API Error')));

      component.loadStories(1);

      expect(component.loading()).toBe(false);
      expect(component.error()).toBe('Failed to load stories. Please try again.');
      expect(component.stories()).toEqual([]);
    });
  });

  describe('component template integration', () => {
    it('should display stories in template', () => {
      component.ngOnInit();
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('.stories-grid')).toBeTruthy();
    });

    it('should show loading state', () => {
      component.ngOnInit();
      fixture.detectChanges();
      
      component.loading.set(true);
      component.stories.set([]);
      component.error.set(null);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('.loading-section')).toBeTruthy();
      expect(compiled.textContent).toContain('Loading stories...');
    });

    it('should show error state', () => {
      component.ngOnInit();
      fixture.detectChanges();
      
      component.error.set('Test error message');
      component.loading.set(false);
      component.stories.set([]);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('.error-section')).toBeTruthy();
      expect(compiled.textContent).toContain('Test error message');
    });

    it('should show search section', () => {
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.querySelector('.search-section')).toBeTruthy();
      expect(compiled.querySelector('.search-field')).toBeTruthy();
    });
  });
});

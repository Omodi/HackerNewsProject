import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSliderModule } from '@angular/material/slider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HackerNewsService } from '../../services/hackernews.service';
import { Story, SearchQuery, SearchFilters, SearchSortOrder } from '../../models/story.model';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-story-list',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatToolbarModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatAutocompleteModule,
    MatExpansionModule,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSliderModule,
    MatTooltipModule
  ],
  templateUrl: './story-list.html',
  styleUrl: './story-list.scss'
})
export class StoryList implements OnInit {
  private readonly hackerNewsService = inject(HackerNewsService);
  private readonly searchSubject = new Subject<string>();
  private readonly fb = inject(FormBuilder);

  stories = signal<Story[]>([]);
  currentPage = signal<number>(1);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  searchQuery = signal<string>('');
  isSearchMode = signal<boolean>(false);
  suggestions = signal<string[]>([]);
  loadingSuggestions = signal<boolean>(false);
  filtersExpanded = signal<boolean>(false);
  hasNextPage = signal<boolean>(true);
  currentPageSize = signal<number>(20);

  filtersForm: FormGroup;

  SearchSortOrder = SearchSortOrder;
  sortOptions = [
    { value: SearchSortOrder.Relevance, label: 'Relevance' },
    { value: SearchSortOrder.Score, label: 'Score' },
    { value: SearchSortOrder.Recent, label: 'Most Recent' },
    { value: SearchSortOrder.Oldest, label: 'Oldest' },
    { value: SearchSortOrder.Comments, label: 'Most Comments' }
  ];

  constructor() {
    this.filtersForm = this.fb.group({
      fromDate: [null],
      toDate: [null],
      minScore: [null],
      maxScore: [null],
      author: [''],
      domain: [''],
      hasUrl: [null],
      sortBy: [SearchSortOrder.Relevance]
    });
  }

  ngOnInit() {
    this.loadStories();
    this.setupSearchSuggestions();
  }

  private setupSearchSuggestions() {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (query.length < 2) {
          return of([]);
        }
        this.loadingSuggestions.set(true);
        return this.hackerNewsService.getSearchSuggestions(query);
      })
    ).subscribe({
      next: (suggestions) => {
        this.suggestions.set(suggestions);
        this.loadingSuggestions.set(false);
      },
      error: (err) => {
        console.error('Error loading suggestions:', err);
        this.suggestions.set([]);
        this.loadingSuggestions.set(false);
      }
    });
  }

  loadStories(page: number = 1) {
    this.loading.set(true);
    this.error.set(null);
    
    const query = this.searchQuery();
    
    if (query || this.hasActiveFilters()) {
      const searchQuery: SearchQuery = {
        query: query || '',
        page: page,
        pageSize: 20,
        sortBy: this.filtersForm.get('sortBy')?.value || SearchSortOrder.Relevance,
        filters: this.buildFilters()
      };

      this.hackerNewsService.searchStories(searchQuery).subscribe({
        next: (result) => {
          console.log(`Search results for page ${page}:`, result.items.length, 'items');
          if (result.items.length === 0 && page > 1) {
            // No results on this page, stay on previous page and disable next
            console.log('No results found, staying on previous page');
            this.currentPage.set(page - 1);
            this.hasNextPage.set(false);
            this.loading.set(false);
            return;
          }
          this.currentPage.set(page);
          this.stories.set(result.items);
          this.currentPageSize.set(result.pageSize);
          // If we got fewer items than requested, we're at the end
          this.hasNextPage.set(result.items.length === result.pageSize);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error loading stories:', err);
          this.error.set('Failed to load stories. Please try again.');
          this.loading.set(false);
        }
      });
    } else {
      this.hackerNewsService.getStories(page, 20).subscribe({
        next: (result) => {
          console.log(`Stories for page ${page}:`, result.items.length, 'items');
          if (result.items.length === 0 && page > 1) {
            // No results on this page, stay on previous page and disable next
            console.log('No results found, staying on previous page');
            this.currentPage.set(page - 1);
            this.hasNextPage.set(false);
            this.loading.set(false);
            return;
          }
          this.currentPage.set(page);
          this.stories.set(result.items);
          this.currentPageSize.set(result.pageSize);
          // If we got fewer items than requested, we're at the end
          this.hasNextPage.set(result.items.length === result.pageSize);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error loading stories:', err);
          this.error.set('Failed to load stories. Please try again.');
          this.loading.set(false);
        }
      });
    }
  }

  private buildFilters(): SearchFilters | undefined {
    const formValue = this.filtersForm.value;
    const filters: SearchFilters = {};
    let hasFilters = false;

    if (formValue.fromDate) {
      filters.fromDate = formValue.fromDate;
      hasFilters = true;
    }
    if (formValue.toDate) {
      filters.toDate = formValue.toDate;
      hasFilters = true;
    }
    if (formValue.minScore !== null && formValue.minScore !== '') {
      filters.minScore = Number(formValue.minScore);
      hasFilters = true;
    }
    if (formValue.maxScore !== null && formValue.maxScore !== '') {
      filters.maxScore = Number(formValue.maxScore);
      hasFilters = true;
    }
    if (formValue.author?.trim()) {
      filters.author = formValue.author.trim();
      hasFilters = true;
    }
    if (formValue.domain?.trim()) {
      filters.domain = formValue.domain.trim();
      hasFilters = true;
    }
    if (formValue.hasUrl !== null) {
      filters.hasUrl = formValue.hasUrl;
      hasFilters = true;
    }

    return hasFilters ? filters : undefined;
  }

  private hasActiveFilters(): boolean {
    const formValue = this.filtersForm.value;
    return !!(
      formValue.fromDate ||
      formValue.toDate ||
      (formValue.minScore !== null && formValue.minScore !== '') ||
      (formValue.maxScore !== null && formValue.maxScore !== '') ||
      formValue.author?.trim() ||
      formValue.domain?.trim() ||
      formValue.hasUrl !== null ||
      formValue.sortBy !== SearchSortOrder.Relevance
    );
  }

  goToNextPage() {
    if (this.hasNextPage()) {
      this.loadStories(this.currentPage() + 1);
    }
  }

  goToPreviousPage() {
    if (this.currentPage() > 1) {
      this.loadStories(this.currentPage() - 1);
    }
  }

  formatTimeAgo(date: Date | string | number): string {
    const now = new Date();
    let targetDate: Date;
    
    if (typeof date === 'number') {
      // Unix timestamp (in seconds), convert to milliseconds
      targetDate = new Date(date * 1000);
    } else if (typeof date === 'string') {
      targetDate = new Date(date);
    } else {
      targetDate = date;
    }
    
    // Handle invalid dates
    if (isNaN(targetDate.getTime())) {
      return 'unknown';
    }
    
    const diffInMinutes = Math.floor((now.getTime() - targetDate.getTime()) / (1000 * 60));
    
    if (diffInMinutes < 1) return 'just now';
    if (diffInMinutes < 60) return `${diffInMinutes}m ago`;
    
    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) return `${diffInHours}h ago`;
    
    const diffInDays = Math.floor(diffInHours / 24);
    if (diffInDays < 30) return `${diffInDays}d ago`;
    
    const diffInMonths = Math.floor(diffInDays / 30);
    if (diffInMonths < 12) return `${diffInMonths}mo ago`;
    
    const diffInYears = Math.floor(diffInMonths / 12);
    return `${diffInYears}y ago`;
  }

  getDomainFromUrl(url: string | undefined): string {
    if (!url) return '';
    try {
      return new URL(url).hostname.replace('www.', '');
    } catch {
      return '';
    }
  }

  onSearchInput(event: Event) {
    const target = event.target as HTMLInputElement;
    const query = target.value;
    this.searchQuery.set(query);
    
    if (query.length >= 2) {
      this.searchSubject.next(query);
    } else {
      this.suggestions.set([]);
    }
  }

  onSearch(query: string) {
    this.searchQuery.set(query.trim());
    this.isSearchMode.set(query.trim().length > 0 || this.hasActiveFilters());
    this.currentPage.set(1);
    this.loadStories(1);
  }

  onFiltersChanged() {
    this.isSearchMode.set(this.searchQuery().trim().length > 0 || this.hasActiveFilters());
    this.currentPage.set(1);
    this.loadStories(1);
  }

  clearAllFilters() {
    this.filtersForm.reset({
      sortBy: SearchSortOrder.Relevance
    });
    this.onFiltersChanged();
  }

  toggleFilters() {
    this.filtersExpanded.set(!this.filtersExpanded());
  }

  getActiveFiltersCount(): number {
    const formValue = this.filtersForm.value;
    let count = 0;
    
    if (formValue.fromDate) count++;
    if (formValue.toDate) count++;
    if (formValue.minScore !== null && formValue.minScore !== '') count++;
    if (formValue.maxScore !== null && formValue.maxScore !== '') count++;
    if (formValue.author?.trim()) count++;
    if (formValue.domain?.trim()) count++;
    if (formValue.hasUrl !== null) count++;
    if (formValue.sortBy !== SearchSortOrder.Relevance) count++;
    
    return count;
  }

  displaySuggestion(suggestion: string): string {
    return suggestion || '';
  }

  onSuggestionSelected(event: MatAutocompleteSelectedEvent) {
    const suggestion = event.option.value;
    this.searchQuery.set(suggestion);
    this.onSearch(suggestion);
  }

  clearSearch() {
    this.searchQuery.set('');
    this.suggestions.set([]);
    this.filtersForm.reset({
      sortBy: SearchSortOrder.Relevance
    });
    this.isSearchMode.set(false);
    this.currentPage.set(1);
    this.hasNextPage.set(true);
    this.loadStories(1);
  }
}

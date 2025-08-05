import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Story, PagedStoryResult, SearchQuery, SearchSortOrder } from '../models/story.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class HackerNewsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getStories(pageNumber: number = 1, pageSize: number = 20): Observable<PagedStoryResult> {
    const params = new HttpParams()
      .set('page', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedStoryResult>(`${this.baseUrl}/stories`, { params })
      .pipe(
        map(response => ({
          ...response,
          items: response.items.map(story => this.normalizeStory(story))
        }))
      );
  }

  searchStories(searchQuery: SearchQuery): Observable<PagedStoryResult> {
    let params = new HttpParams()
      .set('q', searchQuery.query || '')
      .set('page', (searchQuery.page || 1).toString())
      .set('pageSize', (searchQuery.pageSize || 20).toString())
      .set('sortBy', searchQuery.sortBy || SearchSortOrder.Relevance);

    if (searchQuery.filters) {
      const filters = searchQuery.filters;
      
      if (filters.fromDate) {
        params = params.set('fromDate', filters.fromDate);
      }
      if (filters.toDate) {
        params = params.set('toDate', filters.toDate);
      }
      if (filters.minScore !== undefined) {
        params = params.set('minScore', filters.minScore.toString());
      }
      if (filters.maxScore !== undefined) {
        params = params.set('maxScore', filters.maxScore.toString());
      }
      if (filters.author) {
        params = params.set('author', filters.author);
      }
      if (filters.domain) {
        params = params.set('domain', filters.domain);
      }
      if (filters.hasUrl !== undefined) {
        params = params.set('hasUrl', filters.hasUrl.toString());
      }
    }

    return this.http.get<PagedStoryResult>(`${this.baseUrl}/search`, { params })
      .pipe(
        map(response => ({
          ...response,
          items: response.items.map(story => this.normalizeStory(story))
        }))
      );
  }

  getSearchSuggestions(query: string, limit: number = 10): Observable<string[]> {
    const params = new HttpParams()
      .set('q', query)
      .set('limit', limit.toString());

    return this.http.get<string[]>(`${this.baseUrl}/search/suggestions`, { params });
  }

  private normalizeStory(apiStory: any): Story {
    return {
      ...apiStory,
      commentCount: apiStory.commentCount || apiStory.descendants || 0,
      hackerNewsUrl: apiStory.hackerNewsUrl || `https://news.ycombinator.com/item?id=${apiStory.id}`
    };
  }

}
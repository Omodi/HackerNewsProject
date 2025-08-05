export interface Story {
  id: number;
  title: string;
  by: string;
  time: Date;
  createdAt: Date;
  score: number;
  url?: string;
  type: string;
  hasUrl: boolean;
  kids?: number[];
  descendants?: number;
  commentCount: number;
  hackerNewsUrl: string;
}

export interface StoryApiResponse {
  id: number;
  title: string;
  by: string;
  time: number;
  createdAt: string;
  score: number;
  url?: string;
  type: string;
  hasUrl: boolean;
  kids?: number[];
  descendants?: number;
  commentCount: number;
  hackerNewsUrl: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedStoryResult extends PagedResult<Story> {}

export enum SearchSortOrder {
  Relevance = 'Relevance',
  Score = 'Score',
  Recent = 'Recent',
  Oldest = 'Oldest',
  Comments = 'Comments'
}

export interface SearchFilters {
  fromDate?: Date;
  toDate?: Date;
  minScore?: number;
  maxScore?: number;
  author?: string;
  domain?: string;
  hasUrl?: boolean;
}

export interface SearchQuery {
  query: string;
  page?: number;
  pageSize?: number;
  sortBy?: SearchSortOrder;
  filters?: SearchFilters;
}
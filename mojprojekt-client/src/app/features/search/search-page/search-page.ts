import { Component, inject, signal } from '@angular/core';
import { SearchApiService } from '../../../core/services/search-api.service';
import { SearchResponse } from '../../../core/models/search.model';
import { SearchBar } from '../search-bar/search-bar';
import { SearchResultsList } from '../search-results-list/search-results-list';
import { CrawlStatusBanner } from '../../crawl-status/crawl-status-banner';

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [SearchBar, SearchResultsList, CrawlStatusBanner],
  templateUrl: './search-page.html',
  styleUrl: './search-page.css'
})
export class SearchPage {
  private readonly searchApi = inject(SearchApiService);

  readonly isSearching = signal(false);
  readonly response = signal<SearchResponse | null>(null);
  readonly hasSearched = signal(false);
  readonly error = signal<string | null>(null);

  onSearch(query: string): void {
    this.isSearching.set(true);
    this.error.set(null);

    this.searchApi.search(query).subscribe({
      next: (response) => {
        this.response.set(response);
        this.hasSearched.set(true);
        this.isSearching.set(false);
      },
      error: () => {
        this.isSearching.set(false);
        this.hasSearched.set(true);
        this.error.set('Search failed. Make sure the backend is running and try again.');
      }
    });
  }
}

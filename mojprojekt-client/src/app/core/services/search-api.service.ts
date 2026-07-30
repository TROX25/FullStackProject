import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { SearchResponse } from '../models/search.model';

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);

  search(query: string, maxResults = 20): Observable<SearchResponse> {
    return this.http.post<SearchResponse>(`${API_BASE_URL}/search`, { query, maxResults });
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { ListingSummary, PagedResult } from '../models/listing.model';

@Injectable({ providedIn: 'root' })
export class ListingsApiService {
  private readonly http = inject(HttpClient);

  getListings(page = 1, pageSize = 20): Observable<PagedResult<ListingSummary>> {
    return this.http.get<PagedResult<ListingSummary>>(
      `${API_BASE_URL}/listings?page=${page}&pageSize=${pageSize}`
    );
  }
}

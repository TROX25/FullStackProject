import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './api-config';
import { CrawlRun } from '../models/crawl.model';

@Injectable({ providedIn: 'root' })
export class CrawlApiService {
  private readonly http = inject(HttpClient);

  triggerCrawl(): Observable<{ crawlRunId: string; status: string }> {
    return this.http.post<{ crawlRunId: string; status: string }>(`${API_BASE_URL}/crawl`, {});
  }

  getLatest(): Observable<CrawlRun> {
    return this.http.get<CrawlRun>(`${API_BASE_URL}/crawl/latest`);
  }

  getById(id: string): Observable<CrawlRun> {
    return this.http.get<CrawlRun>(`${API_BASE_URL}/crawl/${id}`);
  }
}

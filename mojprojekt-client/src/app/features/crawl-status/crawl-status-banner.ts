import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { interval, switchMap, takeWhile, catchError, of } from 'rxjs';
import { CrawlApiService } from '../../core/services/crawl-api.service';
import { CrawlRun } from '../../core/models/crawl.model';

@Component({
  selector: 'app-crawl-status-banner',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './crawl-status-banner.html',
  styleUrl: './crawl-status-banner.css'
})
export class CrawlStatusBanner implements OnInit {
  private readonly crawlApi = inject(CrawlApiService);

  readonly latestRun = signal<CrawlRun | null>(null);
  readonly isPolling = signal(false);
  readonly hasCheckedOnce = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.crawlApi.getLatest().subscribe({
      next: (run) => {
        this.latestRun.set(run);
        this.hasCheckedOnce.set(true);
      },
      error: () => {
        // No crawl has ever run yet (404) — that's an expected first-run state, not an error.
        this.hasCheckedOnce.set(true);
      }
    });
  }

  refresh(): void {
    this.error.set(null);
    this.isPolling.set(true);

    this.crawlApi.triggerCrawl().subscribe({
      next: ({ crawlRunId }) => this.pollUntilDone(crawlRunId),
      error: () => {
        this.isPolling.set(false);
        this.error.set('Could not start a crawl. Is the backend running?');
      }
    });
  }

  private pollUntilDone(crawlRunId: string): void {
    interval(1000)
      .pipe(
        switchMap(() => this.crawlApi.getById(crawlRunId)),
        catchError(() => of(null)),
        takeWhile((run) => run === null || run.status === 'Pending' || run.status === 'Running', true)
      )
      .subscribe((run) => {
        if (run) {
          this.latestRun.set(run);
        }
        if (!run || run.status === 'Completed' || run.status === 'Failed') {
          this.isPolling.set(false);
        }
      });
  }
}

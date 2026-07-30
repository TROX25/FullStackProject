export type CrawlStatus = 'Pending' | 'Running' | 'Completed' | 'Failed';
export type CrawlSourceUsed = 'None' | 'Live' | 'Fallback';

export interface CrawlRun {
  id: string;
  status: CrawlStatus;
  sourceUsed: CrawlSourceUsed;
  listingsFound: number;
  startedAt: string;
  completedAt: string | null;
  errorMessage: string | null;
}

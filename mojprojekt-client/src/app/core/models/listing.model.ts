export interface ListingSummary {
  id: string;
  title: string;
  priceAmount: number;
  currency: string;
  year: number;
  mileage: number | null;
  transmission: string;
  fuelType: string;
  bodyType: string;
  brand: string;
  model: string;
  city: string;
  publishedAt: string;
  thumbnailUrl: string | null;
  sourceUrl: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

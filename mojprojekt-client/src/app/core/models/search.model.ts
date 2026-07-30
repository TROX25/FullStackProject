import { ListingSummary } from './listing.model';

export interface InterpretedCriteria {
  priceMin: number | null;
  priceMax: number | null;
  yearMin: number | null;
  yearMax: number | null;
  mileageMax: number | null;
  transmission: string | null;
  transmissionRequired: boolean;
  fuelType: string | null;
  fuelTypeRequired: boolean;
  bodyType: string | null;
  bodyTypeRequired: boolean;
  brand: string | null;
  model: string | null;
  keywords: string[];
}

export interface SearchResultItem {
  listing: ListingSummary;
  score: number;
  matchReasons: string[];
  unmetPreferences: string[];
}

export interface SearchResponse {
  intentSummary: string;
  interpretedCriteria: InterpretedCriteria;
  warnings: string[];
  results: SearchResultItem[];
}

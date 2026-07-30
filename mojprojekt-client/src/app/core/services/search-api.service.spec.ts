import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SearchApiService } from './search-api.service';
import { API_BASE_URL } from './api-config';
import { SearchResponse } from '../models/search.model';

describe('SearchApiService', () => {
  let service: SearchApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SearchApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('posts the query and maxResults to /api/search and returns the response', () => {
    const mockResponse: SearchResponse = {
      intentSummary: 'Looking for a cheap car.',
      interpretedCriteria: {
        priceMin: null,
        priceMax: 20000,
        yearMin: null,
        yearMax: null,
        mileageMax: null,
        transmission: null,
        transmissionRequired: false,
        fuelType: null,
        fuelTypeRequired: false,
        bodyType: null,
        bodyTypeRequired: false,
        brand: null,
        model: null,
        keywords: [],
      },
      warnings: [],
      results: [],
    };

    let actual: SearchResponse | undefined;
    service.search('cheap car', 15).subscribe((response) => (actual = response));

    const req = httpMock.expectOne(`${API_BASE_URL}/search`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ query: 'cheap car', maxResults: 15 });

    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });

  it('defaults maxResults to 20 when not specified', () => {
    service.search('anything').subscribe();

    const req = httpMock.expectOne(`${API_BASE_URL}/search`);
    expect(req.request.body.maxResults).toBe(20);
    req.flush({ intentSummary: '', interpretedCriteria: {}, warnings: [], results: [] });
  });
});

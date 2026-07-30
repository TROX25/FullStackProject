import { TestBed } from '@angular/core/testing';
import { SearchResultCard } from './search-result-card';
import { SearchResultItem } from '../../../core/models/search.model';

describe('SearchResultCard', () => {
  const result: SearchResultItem = {
    listing: {
      id: '1',
      title: 'Skoda Octavia 2019 Estate',
      priceAmount: 45900,
      currency: 'Pln',
      year: 2019,
      mileage: 90000,
      transmission: 'Manual',
      fuelType: 'Diesel',
      bodyType: 'Estate',
      brand: 'Skoda',
      model: 'Octavia',
      city: 'Warszawa',
      publishedAt: new Date().toISOString(),
      thumbnailUrl: null,
      sourceUrl: 'https://example.test/listing',
    },
    score: 87,
    matchReasons: ['Within your budget of 60,000 Pln.'],
    unmetPreferences: ['Manual transmission — you preferred Automatic.'],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [SearchResultCard] }).compileComponents();
  });

  it('renders the listing title and score', () => {
    const fixture = TestBed.createComponent(SearchResultCard);
    fixture.componentRef.setInput('result', result);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Skoda Octavia 2019 Estate');
    expect(text).toContain('87');
  });

  it('hides match reasons until toggled, then shows them', () => {
    const fixture = TestBed.createComponent(SearchResultCard);
    fixture.componentRef.setInput('result', result);
    fixture.detectChanges();

    let text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('Within your budget');

    const toggle = (fixture.nativeElement as HTMLElement).querySelector('.card__toggle') as HTMLButtonElement;
    toggle.click();
    fixture.detectChanges();

    text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Within your budget');
    expect(text).toContain('Manual transmission — you preferred Automatic.');
  });
});

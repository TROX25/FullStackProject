import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

const EXAMPLE_QUERIES = [
  'A reliable family estate under PLN 60,000, preferably automatic and no older than 2019',
  'Cheap small hatchback for the city, under 25000 PLN',
  'Diesel SUV, automatic, mileage under 100000 km',
];

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './search-bar.html',
  styleUrl: './search-bar.css'
})
export class SearchBar {
  @Input() isSearching = false;
  @Output() search = new EventEmitter<string>();

  readonly query = signal('');
  readonly exampleQueries = EXAMPLE_QUERIES;

  submit(): void {
    const value = this.query().trim();
    if (value.length > 0) {
      this.search.emit(value);
    }
  }

  useExample(example: string): void {
    this.query.set(example);
    this.search.emit(example);
  }
}

import { Component, Input } from '@angular/core';
import { SearchResultCard } from '../search-result-card/search-result-card';
import { SearchResultItem } from '../../../core/models/search.model';

@Component({
  selector: 'app-search-results-list',
  standalone: true,
  imports: [SearchResultCard],
  templateUrl: './search-results-list.html',
  styleUrl: './search-results-list.css'
})
export class SearchResultsList {
  @Input({ required: true }) results!: SearchResultItem[];
}

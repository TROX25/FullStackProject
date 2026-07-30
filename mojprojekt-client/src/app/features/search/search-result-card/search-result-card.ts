import { Component, Input, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { SearchResultItem } from '../../../core/models/search.model';

@Component({
  selector: 'app-search-result-card',
  standalone: true,
  imports: [DecimalPipe],
  templateUrl: './search-result-card.html',
  styleUrl: './search-result-card.css'
})
export class SearchResultCard {
  @Input({ required: true }) result!: SearchResultItem;

  readonly expanded = signal(false);

  toggleExpanded(): void {
    this.expanded.set(!this.expanded());
  }
}

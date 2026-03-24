import { Component, inject } from '@angular/core';
import { LoadingService } from '../../services/ui.services';

@Component({
  selector: 'app-spinner',
  standalone: true,
  imports: [],
  templateUrl: './spinner.component.html',
  styleUrls: ['./spinner.component.css']
})
export class SpinnerComponent {
  readonly loading = inject(LoadingService);
}

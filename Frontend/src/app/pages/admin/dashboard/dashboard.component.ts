import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../services/admin.service';
import { ToastService } from '../../../services/ui.services';
import { AdminStatsDto } from '../../../models/admin.models';
import { AdminNavComponent } from '../../../components/admin-nav/admin-nav.component';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [RouterLink, AdminNavComponent],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  readonly adminSvc = inject(AdminService);
  readonly toast    = inject(ToastService);

  stats   = signal<AdminStatsDto | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    this.adminSvc.getStats().subscribe({
      next: s => { this.stats.set(s); this.loading.set(false); },
      error: () => { this.toast.error('Failed to load stats.'); this.loading.set(false); }
    });
  }
}

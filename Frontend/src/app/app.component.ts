import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent }     from './components/navbar/navbar.component';
import { FooterComponent }     from './components/footer/footer.component';
import { SpinnerComponent }    from './components/spinner/spinner.component';
import { ToastComponent }      from './components/toast/toast.component';
import { LoginModalComponent } from './components/login-modal/login-modal.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    NavbarComponent,
    FooterComponent,
    SpinnerComponent,
    ToastComponent,
    LoginModalComponent
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  readonly title = 'inkwell';
}

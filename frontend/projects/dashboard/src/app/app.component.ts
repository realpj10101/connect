import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { RegisterComponent } from "./components/account/register/register.component";
import { LoginComponent } from "./components/account/login/login.component";
import { AccountService } from './services/account.service';
import { NavbarComponent } from "./components/navbar/navbar.component";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private _accountService = inject(AccountService);
  router = inject(Router);

  constructor() {
    this.initUserOnPageRefresh();
  }

  initUserOnPageRefresh(): void {
    const loggedInPlayerStr = localStorage.getItem('loggedInUser');

    if (loggedInPlayerStr) {
      // First, check if user's token is not expired.
      this._accountService.authorizeLoggedInUser();

      // Then, set the authorized logged-in user
      this._accountService.setCurrentUser(JSON.parse(loggedInPlayerStr))
    }
  }
}

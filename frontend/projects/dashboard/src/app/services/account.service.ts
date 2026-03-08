import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { LoggedInUser } from '../models/logged-in-user.model';
import { RegiserReq } from '../models/register.model';
import { map, Observable, retry, take } from 'rxjs';
import { Router } from '@angular/router';
import { ApiResponse } from '../models/helpers/api-response.model';
import { LoginReq } from '../models/login.model';

@Injectable({
  providedIn: 'root'
})
export class AccountService {
  private _http = inject(HttpClient);
  private _router = inject(Router);
  private _loggedInUserSig = signal<LoggedInUser | null>(null);
  
  private readonly _baseApiUrl = environment.apiUrl + 'api/account/';

  regiser(req: RegiserReq): Observable<LoggedInUser | null> {
    return this._http.post<LoggedInUser>(this._baseApiUrl + 'register', req).pipe(
      map(res => {
        if (res) {
          this.setCurrentUser(res);

          this.navigateToRetutnUrl();

          return res;
        }

        return null;
      })
    )    
  } 

  login(req: LoginReq): Observable<LoggedInUser | null> {
    return this._http.post<LoggedInUser>(this._baseApiUrl + 'login', req).pipe(
      map(res => {
        if (res) {
          this.setCurrentUser(res);

          this.navigateToRetutnUrl();

          return res;
        }

        return null;
      })
    )
  }

  authorizeLoggedInUser(): void {
    this._http.get<ApiResponse>(this._baseApiUrl)
      .pipe(take(1))
        .subscribe({
          error: (err) => {
            this.logout();
          }
        })
  }

  setCurrentUser(loggedInUser: LoggedInUser): void {
    this.setLoggedInUserRoles(loggedInUser);

    this._loggedInUserSig.set(loggedInUser);

    localStorage.setItem('loggedInUser', JSON.stringify(loggedInUser));
  }

  setLoggedInUserRoles(loggedInUser: LoggedInUser): void {
    loggedInUser.roles = [];

    const roles: string | string[] = JSON.parse(atob(loggedInUser.token.split('.')[1])).role;

    Array.isArray(roles) ? loggedInUser.roles = roles : loggedInUser.roles.push(roles);
  }

  logout(): void {
    this._loggedInUserSig.set(null);

    localStorage.clear();

    this._router.navigateByUrl('/sign-in');
  }

  private navigateToRetutnUrl(): void {
    const returnUrl = localStorage.getItem('returnUrl');

    if (returnUrl)
      this._router.navigate([returnUrl]);
    else
        this._router.navigate(['dashboard']);

    localStorage.removeItem('returnUrl');
  }
}

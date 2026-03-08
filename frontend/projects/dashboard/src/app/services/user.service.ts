import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { Observable } from 'rxjs';
import { User } from '../models/user.mode';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private _http = inject(HttpClient);

  private readonly  _baseApiUrl = environment.apiUrl + 'api/user/';

  getUserById(): Observable<User> {
    return this._http.get<User>(this._baseApiUrl + 'get-user-by-id');
  }
}

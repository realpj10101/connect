import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/helpers/api-response.model';
import { Member } from '../models/member.model';
import { environment } from '../../environments/environment.development';
import { HttpClient } from '@angular/common/http';
import { RoomResponse } from '../models/room.model';

@Injectable({
  providedIn: 'root'
})
export class RoomMembershipService {
  private _http = inject(HttpClient);

  private readonly _baseApiUrl = environment.apiUrl + 'api/roommembership/';

  joinRoom(roomId: string): Observable<ApiResponse> {
    return this._http.put<ApiResponse>(this._baseApiUrl + "join/" + roomId, null);
  }

  leaveRoom(roomId: string): Observable<ApiResponse> {
    return this._http.put<ApiResponse>(this._baseApiUrl + 'leave/' + roomId, null);
  }

  removeMember(roomId: string, targetUserName: string): Observable<ApiResponse> {
    return this._http.put<ApiResponse>(this._baseApiUrl + 'remove-member/' + roomId + '/' + targetUserName, null);
  }

  getRoomMembers(roomId: string): Observable<Member[]> {
    return this._http.get<Member[]>(this._baseApiUrl + 'get-room-members/' + roomId);
  }

  getRoomUserIsMemberOf(): Observable<RoomResponse[]> {
    return this._http.get<RoomResponse[]>(this._baseApiUrl + 'get-rooms-user-is-member');
  }
}

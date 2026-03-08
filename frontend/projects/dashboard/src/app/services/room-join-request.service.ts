import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/helpers/api-response.model';
import { MembershipProposalsResponse } from '../models/membership-proposal-response.model';

@Injectable({
  providedIn: 'root'
})
export class RoomJoinRequestService {
  private _http = inject(HttpClient);

  private readonly _baseApiUrl = environment.apiUrl + 'api/roomjoinrequest/';

  joinRequestRoom(roomId: string): Observable<ApiResponse> {
    return this._http.post<ApiResponse>(this._baseApiUrl + 'join-request/' + roomId, null);
  }

  getAllJoinRequests(roomId: string): Observable<MembershipProposalsResponse[]> {
    return this._http.get<MembershipProposalsResponse[]>(this._baseApiUrl + 'get-all-join-requests/' + roomId);
  }

  approveRequest(requestId: string): Observable<ApiResponse> {
    return this._http.put<ApiResponse>(this._baseApiUrl + 'approve-request/' + requestId, null);
  }

  rejectRequest(requestId: string): Observable<ApiResponse> {
    return this._http.put<ApiResponse>(this._baseApiUrl + 'reject-request/' + requestId, null);
  }
}

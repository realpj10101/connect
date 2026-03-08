import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { PaginationHandler } from '../extensions/paginationHandler';
import { PaginationParams } from '../models/helpers/paginationParams.model';
import { Observable } from 'rxjs';
import { PaginatedResult } from '../models/helpers/paginatedResult';
import { RoomResponse } from '../models/room.model';
import { ApiResponse } from '../models/helpers/api-response.model';
import { RoomParams } from '../models/helpers/roomParams.model';
import { MessageRes } from '../models/message.model';
import { Member } from '../models/member.model';
import { UpdateRoom } from '../models/update-room.model';
import { MembershipProposalsResponse } from '../models/membership-proposal-response.model';
import { CreateRoom } from '../models/create-room.model';

@Injectable({
  providedIn: 'root'
})
export class RoomManagementService {
  private _http = inject(HttpClient);

  private readonly _baseApiUrl = environment.apiUrl + 'api/roommanagement/';
  private _paginationHandler = new PaginationHandler();

  createRoom(req: CreateRoom): Observable<ApiResponse> {
    return this._http.post<ApiResponse>(this._baseApiUrl + 'create', req);
  }

  getAll(roomParams: RoomParams): Observable<PaginatedResult<RoomResponse[]>> {
    const params = this.getHttpParams(roomParams);

    return this._paginationHandler.getPaginatedResult<RoomResponse[]>(this._baseApiUrl, params);
  }

  getRoomById(roomId: string): Observable<RoomResponse> {
    return this._http.get<RoomResponse>(this._baseApiUrl + 'get-room-by-id/' + roomId);
  }

  updateRoom(roomId: string, req: UpdateRoom): Observable<ApiResponse> {
    return this._http.put<ApiResponse>(this._baseApiUrl + 'update-room/' + roomId, req);
  }

  getRoomsCreatedByUser(): Observable<RoomResponse[]> {
    return this._http.get<RoomResponse[]>(this._baseApiUrl + 'get-rooms-created-by-user');
  }

  private getHttpParams(roomParams: RoomParams): HttpParams {
    let params = new HttpParams();

    if (roomParams) {
      params = params.append('pageSize', roomParams.pageSize);
      params = params.append('pageNumber', roomParams.pageNumber);
      params = params.append('orderBy', roomParams.orderBy);
      params = params.append('search', roomParams.search);
    }

    return params;
  }
}

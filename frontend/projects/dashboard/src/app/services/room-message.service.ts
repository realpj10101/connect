import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { HttpClient, HttpEvent, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MessagePage, MessageRes } from '../models/message.model';
import { MessageParams } from '../models/helpers/messageParams.model';

@Injectable({
  providedIn: 'root'
})
export class RoomMessageService {
  private _http = inject(HttpClient);

  private readonly _baseApiUrl = environment.apiUrl + 'api/roommessage/';

  getRoomMessages(roomId: string, messageParams: MessageParams): Observable<MessagePage> {
    let params = new HttpParams();

    params = params.append('limit', messageParams.limit);

    if (messageParams.lastMessageId)
      params = params.append('lastMessageId', messageParams.lastMessageId);

    return this._http.get<MessagePage>(this._baseApiUrl + 'get-room-messages/' + roomId, {
      params: params
    });
  }

  streamMedia(messageId: string, size: string = '256'): Observable<HttpEvent<Blob>> {
    let params = new HttpParams().set('size', size);

    return this._http.get(this._baseApiUrl + 'stream/' + messageId, {
      params: params,
      responseType: 'blob',
      observe: 'events',
      reportProgress: true
    })
  }
}

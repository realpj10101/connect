import { inject, Injectable } from '@angular/core';
import { environment } from '../../environments/environment.development';
import { Observable } from 'rxjs';
import { RoomResponse } from '../models/room.model';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { ChatItem } from '../models/chat-item.model';

@Injectable({
  providedIn: 'root'
})
export class AudioService {
  private _http = inject(HttpClient);

  private readonly _baseApiUrl = environment.apiUrl + 'api/audio/';

  uploadVoice(roomId: string, blob: Blob): Observable<ChatItem> {
    const form = new FormData();
    form.append("voiceFile", blob, "voice.webm");

    return this._http.post<ChatItem>(this._baseApiUrl + 'upload-voice/' + roomId, form);
  }

  uploadAudio(roomId: string, blob: Blob): Observable<ChatItem> {
    const form = new FormData();
    form.append("audioFile", blob, "audio.webm");

    return this._http.post<ChatItem>(this._baseApiUrl + 'upload-audio/' + roomId, form);
  }

  stream(id: string): Observable<HttpEvent<Blob>> {
    return this._http.get<Blob>(this._baseApiUrl + 'stream/' + id, {
      responseType: 'blob' as 'json',
      observe: 'events',
      reportProgress: true
    });
  }
}

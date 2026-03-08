import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from '../../environments/environment.development';
import { MessageReq, MessageRes } from '../models/message.model';

@Injectable({
  providedIn: 'root'
})
export class RoomMessagingService {
  private _hubConnection: HubConnection | undefined;

  private _baseApiUrl = environment.apiUrl;

  async startConnection(token: string) {
    this._hubConnection = new HubConnectionBuilder()
      .withUrl(this._baseApiUrl + 'hubs/room-messaging', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this._hubConnection.onreconnecting(() => {
      console.log("⚠ reconnecting...");
    })

    this._hubConnection.onreconnected(() => {
      console.log("✅ reconnected");
    })

    this._hubConnection.onclose(() => {
      console.log("❌ connection closed");
    })

    return this._hubConnection.start()
      .then(() => {
        console.log('✅ SignalR connected');
      })
      .catch((err) => console.error('SignalR connection error: ', err));
  }

  stopConnection() {
    return this._hubConnection?.stop();
  }

  joinRoom(roomId: string) {
    return this._hubConnection?.invoke("JoinRoom", roomId);
  }

  leaveRoom(roomId: string) {
    return this._hubConnection?.invoke("LeaveRoom", roomId);
  }

  sendMessage(message: MessageReq, roomId: string) {
    return this._hubConnection?.invoke("SendMessageAsync", message, roomId);
  }

  startTyping(roomId: string) {
    return this._hubConnection?.invoke("StartTyping", roomId);
  }

  stopTyping(roomId: string) {
    return this._hubConnection?.invoke("StopTyping", roomId);
  }

  // Event listeners
  onLoadMessages(callBack: (messages: MessageRes[]) => void) {
    this._hubConnection?.on("LoadMessages", callBack);
  }

  onReceiveMessage(callBack: (message: MessageRes) => void) {
    this._hubConnection?.on("ReceiveMessage", callBack);
  }

  onUserJoined(callBack: (userName: string) => void) {
    this._hubConnection?.on("UserJoined", callBack);
  }

  onUserLeaved(callBack: (userName: string) => void) {
    this._hubConnection?.on("UserLeft", callBack);
  }

  onUserTyping(callBack: (userName: string) => void) {
    this._hubConnection?.off("UserTyping");
    this._hubConnection?.on("UserTyping", callBack);
  }

  onUserStoppedTyping(callBack: (userName: string) => void) {
    this._hubConnection?.off("UserStoppedTyping");
    this._hubConnection?.on("UserStoppedTyping", callBack);
  }

  removeListeners(): void {
    this._hubConnection?.off("LoadMessages");
    this._hubConnection?.off("ReceiveMessage");
    this._hubConnection?.off("UserJoined");
    this._hubConnection?.off("UserLeft");
    this._hubConnection?.off("UserTyping");
    this._hubConnection?.off("UserStoppedTyping");
  }
}

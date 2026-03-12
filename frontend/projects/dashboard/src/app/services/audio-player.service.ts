import { inject, Injectable, signal } from '@angular/core';
import { AudioService } from './audio.service';
import { ChatItem } from '../models/chat-item.model';
import { HttpEventType } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class AudioPlayerService {
  private _audioService = inject(AudioService);

  private _currentPlayingMessage: ChatItem | null = null;

  togglePlay(message: ChatItem): void {
    if (!message.audioUrl) {
      this.downloadAudio(message, true);
      return;
    }

    if (message.isPlaying) {
      message.audioRef?.pause();
      message.isPlaying = false;
      this._currentPlayingMessage = null;
      return;
    }

    if (this._currentPlayingMessage && this._currentPlayingMessage !== message) {
      this._currentPlayingMessage.audioRef?.pause();
      this._currentPlayingMessage.isPlaying = false;
    }

    if (!message.audioRef) {
      message.audioRef = new Audio(message.audioUrl);
    }

    message.audioRef.play();
    message.isPlaying = true;
    this._currentPlayingMessage = message;

    message.audioRef.onended = () => {
      message.isPlaying = false;
      if (this._currentPlayingMessage === message) {
        this._currentPlayingMessage = null;
      }
    };
  }

  downloadAudio(message: ChatItem, autoPlay = false) {
    if (message.isDownloading) return;

    message.isDownloading = true;
    message.downloadProgress = 0;

    this._audioService.stream(message.id).subscribe(event => {

      if (event.type === HttpEventType.DownloadProgress) {
        if (event.total) {
          message.downloadProgress = Math.round(
            100 * event.loaded / event.total
          );
        }
      }

      if (event.type === HttpEventType.Response) {

        const blob = event.body!;
        const url = URL.createObjectURL(blob);

        message.audioUrl = url;
        message.isDownloaded = true;
        message.isDownloading = false;
        message.downloadProgress = 100;

        if (autoPlay) {
          if (this._currentPlayingMessage && this._currentPlayingMessage !== message) {
            this._currentPlayingMessage.audioRef?.pause();
            this._currentPlayingMessage.isPlaying = false;
          }

          message.audioRef = new Audio(url);
          message.audioRef.play();
          message.isPlaying = true;
          this._currentPlayingMessage = message;

          message.audioRef.onended = () => {
            message.isPlaying = false;
            if (this._currentPlayingMessage === message) {
              this._currentPlayingMessage = null;
            }
          };
        }
      }
    });
  }
}

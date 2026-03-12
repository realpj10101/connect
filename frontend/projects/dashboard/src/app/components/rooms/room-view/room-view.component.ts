import { AfterViewChecked, Component, ElementRef, inject, NgZone, OnDestroy, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterModule } from '@angular/router';
import { RoomManagementService } from '../../../services/room-management.service';
import { RoomMessagingService } from '../../../services/room-messaging.service';
import { LoggedInUser } from '../../../models/logged-in-user.model';
import { RoomResponse } from '../../../models/room.model';
import { MessageReq, MessageRes } from '../../../models/message.model';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { IntlModule } from 'angular-ecmascript-intl';
import { debounceTime, Subscription, take, tap } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RoomMembershipService } from '../../../services/room-membership.service';
import { RoomMessageService } from '../../../services/room-message.service';
import { MessageParams } from '../../../models/helpers/messageParams.model';
import { ChatItem } from '../../../models/chat-item.model';
import { VoiceTimePipe } from '../../../pipes/voice-time.pipe';
import { FileSizePipe } from '../../../pipes/file-size.pipe';
import { AudioService } from '../../../services/audio.service';
import { HttpEventType } from '@angular/common/http';
import {
  getStrokeOffset,
  scrollToBottom,
  shouldLoadMore,
  getMessagesClasses,
  formatTimer
} from '../../../utils/room-view.utils';
import { AudioPlayerService } from '../../../services/audio-player.service';

@Component({
  selector: 'app-room-view',
  imports: [
    RouterLink, RouterModule, CommonModule, FormsModule,
    ReactiveFormsModule, IntlModule, VoiceTimePipe, FileSizePipe],
  templateUrl: './room-view.component.html',
  styleUrl: './room-view.component.scss'
})
export class RoomViewComponent implements OnInit, OnDestroy {
  @ViewChild('chatContainer') chatContainer!: ElementRef;

  private _roomService = inject(RoomManagementService);
  private _roomMembershipService = inject(RoomMembershipService);
  private _roomMessagesService = inject(RoomMessageService);
  private _roomMessagingService = inject(RoomMessagingService);
  private _audioService = inject(AudioService);
  private _audioPlayerService = inject(AudioPlayerService);
  private _route = inject(ActivatedRoute);
  private _router = inject(Router);
  private _fB = inject(FormBuilder);
  private _snack = inject(MatSnackBar);
  private _ngZone = inject(NgZone);

  private _isTyping = false;
  private _sub: Subscription | undefined;
  private _previousScrollHeight = 0;
  private _currentPlayingMessage: ChatItem | null = null;
  private _mediaRecorder!: MediaRecorder;
  private _chunks: Blob[] = [];
  private _timerInterval!: any;
  private _startTime!: number;

  readonly radius = 14;
  readonly circumference = 2 * Math.PI * this.radius;

  messagesSig = signal<ChatItem[]>([]);
  notificationSig = signal<string | null>(null);
  typingUsersSig = signal<string[]>([]);
  loadingMoreSig = signal(false);

  loggedInUser: LoggedInUser | undefined;
  id: string | undefined | null;
  room: RoomResponse | undefined;
  direction: 'rtl' | 'ltr' = 'ltr';
  messageParams: MessageParams | undefined;
  hasMore = true;
  isRecording = false;
  recordTimer = "00:00";
  selectedAudio: File | null = null;

  inpCtrl = this._fB.control('');

  ngOnInit(): void {
    this.id = this._route.snapshot.paramMap.get('id');

    this.messageParams = new MessageParams();

    if (!this.id) return;

    this.getRoomByIdComp(this.id);
    this.setupTypingStream();

    this.inpCtrl.valueChanges.subscribe(value => {
      if (!value) {
        this.direction = 'ltr';
        return;
      }

      const firstChar = value.trim()[0];
      this.direction = /[\u0600-\u06FF]/.test(firstChar) ? 'rtl' : 'ltr';
    })
  }

  ngOnDestroy(): void {
    if (this.id) {
      this._roomMessagingService.leaveRoom(this.id)
        ?.catch(err => console.log(err)
        ).finally(() => {
          this._roomMessagingService.removeListeners();
          this._roomMessagingService.stopConnection();
        });
    }
    else {
      this._roomMessagingService.removeListeners();
      this._roomMessagingService.stopConnection();
    }

    this._sub?.unsubscribe();
  }

  // Audio & Voice
  async startRecording() {
    if (this.inpCtrl.value) return;

    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

    this._mediaRecorder = new MediaRecorder(stream);
    this._chunks = [];

    this._mediaRecorder.ondataavailable = (e) => this._chunks.push(e.data);

    this._mediaRecorder.start();

    this.isRecording = true;
    this.startTimer();
  }

  stopRecording(send: boolean): void {
    if (!this.isRecording) return;

    this._mediaRecorder.stop();
    clearInterval(this._timerInterval);
    this.isRecording = false;

    this._mediaRecorder.onstop = () => {
      const blob = new Blob(this._chunks, { type: 'audio/webm' });

      if (!send) return;

      this.uploadVoice(blob);
    }
  }

  cancelRecording(): void {
    if (!this.isRecording) return;

    this._mediaRecorder.stop();
    clearInterval(this._timerInterval);
    this.isRecording = false;

    this._chunks = [];
  }

  startTimer(): void {
    this._startTime = Date.now();

    this._timerInterval = setInterval(() => {
      const diff = Date.now() - this._startTime;
      this.recordTimer = formatTimer(diff);
    }, 500);
  }

  uploadVoice(blob: Blob): void {
    if (!this.id) return;

    this._audioService.uploadVoice(this.id, blob).subscribe({
      next: (res) => {
        setTimeout(() => this.scrollToBottom(true));
      },
      error: (err) => {
        this._snack.open(err.error, 'Close', {
          duration: 5000,
          verticalPosition: 'top',
          horizontalPosition: 'center'
        })
      }
    })
  }

  getStrokeOffsetComp(message: ChatItem): number {
    return getStrokeOffset(message);
  }

  togglePlay(message: ChatItem): void {
    this._audioPlayerService.togglePlay(message);
  }

  downloadAudioComp(message: ChatItem, autoPlay = false) {
    this._audioPlayerService.downloadAudio(message, autoPlay);
  }

  onAudioSelected(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) return;

    this.selectedAudio = input.files[0];
  }

  cancelAudio(): void {
    this.selectedAudio = null;
  }

  uploadSelectedAudio(): void {
    if (!this.selectedAudio) return;

    if (this.id) {
      this._audioService.uploadAudio(this.id, this.selectedAudio).subscribe({
        next: (res) => {
          this.selectedAudio = null;
        },
        error: (err) => {
          this._snack.open(err.error, 'Close', {
            horizontalPosition: 'center',
            verticalPosition: 'top',
            duration: 5000
          })
        }
      });
    }
  }

  // Message & Room

  leaveRoomAction(): void {
    if (this.room?.roomType === "Private")
      this._router.navigateByUrl('/dashboard');
  }

  getRoomByIdComp(id: string): void {
    const loggedInUserStr = localStorage.getItem('loggedInUser');
    if (!loggedInUserStr) return;

    this.loggedInUser = JSON.parse(loggedInUserStr);

    if (id) {
      this._roomService.getRoomById(id).subscribe({
        next: async (res) => {
          this.room = res;

          this.getRoomMessages(id);

          if (this.loggedInUser && this.id) {
            await this._roomMessagingService.startConnection(this.loggedInUser.token);
            await this._roomMessagingService.joinRoom(this.id);

            this.setupEventHandlers();
          }
        },
        error: () => {
          this._router.navigateByUrl('/dashboard');
        }
      })
    }
  }

  getRoomMessages(roomId: string): void {
    if (!this.messageParams) return;

    const element = this.chatContainer?.nativeElement;

    if (this.messageParams.lastMessageId && element) {
      this._previousScrollHeight = element.scrollHeight;
    }

    this._roomMessagesService.getRoomMessages(roomId, this.messageParams).subscribe({
      next: (res) => {
        const msgs = res.messages.reverse();

        if (this.messageParams!.lastMessageId === null) {
          this.messagesSig.set(msgs);

          setTimeout(() => {
            this.scrollToBottom(false);
          });

        } else {

          this.messagesSig.update(m => [...msgs, ...m]);

          setTimeout(() => {
            const el = this.chatContainer.nativeElement;
            const newHeight = el.scrollHeight;

            el.scrollTop = newHeight - this._previousScrollHeight;
          });
        }

        this.hasMore = res.hasMore;

        if (msgs.length > 0) {
          this.messageParams!.lastMessageId = msgs[0].id;
        }

        this.loadingMoreSig.set(false);
      },
      error: () => {
        this.loadingMoreSig.set(false);
        this._router.navigateByUrl('/dashboard');
      }
    });
  }

  setupEventHandlers(): void {
    this._roomMessagingService.onReceiveMessage((msgs) => {
      this.messagesSig.update(m => [...m, msgs]);

      this.scrollToBottom(true);
    })

    this._roomMessagingService.onRecieveVoice((msgs) => {
      this.messagesSig.update(m => [...m, msgs]);

      this.scrollToBottom(true);
    })

    this._roomMessagingService.onRecieveAudio((msgs) => {
      console.log(msgs);
      
      this.messagesSig.update(m => [...m, msgs]);

      this.scrollToBottom();
    })

    this._roomMessagingService.onUserJoined((user) => {
      this.showNotification(`${user} joined the chat`);
    });

    this._roomMessagingService.onUserLeaved((user) => {
      this.showNotification(`${user} left the chat`);
    });

    this._roomMessagingService.onUserTyping((user) => {
      if (user === this.loggedInUser?.userName) return;

      this.typingUsersSig.update(users => {
        if (users.includes(user)) return users;
        return [...users, user];
      });
    });

    this._roomMessagingService.onUserStoppedTyping((user) => {
      this.typingUsersSig.update(users => users.filter(u => u !== user));
    })
  }

  sendMessage(): void {
    if (!this.id || !this.loggedInUser) return;

    const msg: MessageReq = {
      message: this.inpCtrl.value ?? ''
    };

    this._roomMessagingService.sendMessage(msg, this.id);

    this.inpCtrl.setValue('');
    this._roomMessagingService.stopTyping(this.id);
  }

  sendMessageIfTextExists(event: SubmitEvent) {
    event.preventDefault();

    if (!this.inpCtrl.value || this.inpCtrl.value.trim() === "") return;

    this.sendMessage();
  }

  getMessagesClasses(userName: string): string {
    return getMessagesClasses(userName, this.loggedInUser?.userName ?? '');
  }

  scrollToBottom(smooth = true): void {
    scrollToBottom(this.chatContainer, this._ngZone, smooth);
  }

  onScroll(): void {
    const element = this.chatContainer.nativeElement;

    if (shouldLoadMore(element, 50) && this.hasMore && !this.loadingMoreSig()) {
      this._previousScrollHeight = element.scrollHeight;
      this.loadingMoreSig.set(true);
      this.getRoomMessages(this.id!);
    }
  }

  showNotification(message: string): void {
    this.notificationSig.set(message);

    setTimeout(() => {
      this.notificationSig.set(null);
    }, 3000);
  }

  leaveRoom(): void {
    if (this.id) {
      this._roomMembershipService.leaveRoom(this.id).subscribe({
        next: (res) => {
          if (this.room)
            this.room.isMember = false

          this._snack.open(res.message, 'Close', {
            duration: 5000,
            verticalPosition: 'top',
            horizontalPosition: 'center'
          })

          this.leaveRoomAction();
        }
      })
    }
  }

  joinRoom(): void {
    if (this.id) {
      this._roomMembershipService.joinRoom(this.id).subscribe({
        next: (res) => {
          if (this.room)
            this.room.isMember = true;

          this._snack.open(res.message, 'Close', {
            duration: 5000,
            verticalPosition: 'top',
            horizontalPosition: 'center'
          })
        }
      })
    }
  }

  private setupTypingStream(): void {
    this._sub = this.inpCtrl.valueChanges.pipe(
      tap(() => {
        if (!this.id) return;

        if (!this._isTyping) {
          this._isTyping = true;
          this._roomMessagingService.startTyping(this.id);
        }
      }),
      debounceTime(1500),
    )
      .subscribe(() => {
        if (!this.id) return;

        this._isTyping = false;
        this._roomMessagingService.stopTyping(this.id);
      })
  }
}

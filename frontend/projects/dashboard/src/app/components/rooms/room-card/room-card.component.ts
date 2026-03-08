import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { RoomResponse } from '../../../models/room.model';
import { CommonModule } from '@angular/common';
import { IntlModule } from "angular-ecmascript-intl";
import { RoomManagementService } from '../../../services/room-management.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink, RouterModule } from '@angular/router';
import { RoomMembershipService } from '../../../services/room-membership.service';
import { RoomJoinRequestService } from '../../../services/room-join-request.service';

@Component({
  selector: 'app-room-card',
  imports: [CommonModule, IntlModule, RouterModule, RouterLink],
  templateUrl: './room-card.component.html',
  styleUrl: './room-card.component.scss'
})
export class RoomCardComponent {
  @Input('roomInput') roomIn: RoomResponse | undefined;
  @Output('leavedRoomOut') leavedRoomOut = new EventEmitter<string>();

  private _roomMembershipService = inject(RoomMembershipService);
  private _roomJoinRequestService = inject(RoomJoinRequestService);
  private _snack = inject(MatSnackBar);
  private _router = inject(Router);

  navigateToRoom(room: RoomResponse): void {
    if (room.roomType === 'Private' && !room.isMember)
      return;

    this._router.navigate(['/room-view/', room.id])
  }

  joinRoomComp(): void {
    if (this.roomIn) {
      this._roomMembershipService.joinRoom(this.roomIn?.id).subscribe({
        next: (res) => {
          if (this.roomIn) {
            this.roomIn.isMember = true;
            this.roomIn.memberCount++
          }
        }
      });
    }
  }

  leaveRoomComp(): void {
    if (this.roomIn) {
      this._roomMembershipService.leaveRoom(this.roomIn?.id).subscribe({
        next: (res) => {
          if (this.roomIn) {
            this.roomIn.isMember = false;
            this.roomIn.memberCount--;
            this.leavedRoomOut.emit(this.roomIn.id);

            this._snack.open(res.message, 'Close', {
              duration: 7000,
              horizontalPosition: 'center',
              verticalPosition: 'top'
            })
          }
        }
      });
    }
  }

  joinRequestRoomComp(): void {
    if (this.roomIn) {
      this._roomJoinRequestService.joinRequestRoom(this.roomIn?.id).subscribe({
        next: (res) => {
          if (this.roomIn)
            this.roomIn.hasPendingRequest = true;
        }
      });
    }
  }
}

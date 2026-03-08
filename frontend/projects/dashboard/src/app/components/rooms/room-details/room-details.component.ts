import { Component, inject, OnInit } from '@angular/core';
import { RoomManagementService } from '../../../services/room-management.service';
import { ActivatedRoute, RouterLink, RouterModule } from '@angular/router';
import { Member } from '../../../models/member.model';
import { RoomResponse } from '../../../models/room.model';
import { CommonModule } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UpdateRoom } from '../../../models/update-room.model';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoomType } from '../../../enums/room-type-enum';
import { MembershipProposalsResponse } from '../../../models/membership-proposal-response.model';
import { RoomMembershipService } from '../../../services/room-membership.service';
import { RoomJoinRequestService } from '../../../services/room-join-request.service';

@Component({
  selector: 'app-room-details',
  imports: [
    RouterLink, RouterModule, CommonModule, ReactiveFormsModule, FormsModule
  ],
  templateUrl: './room-details.component.html',
  styleUrl: './room-details.component.scss'
})
export class RoomDetailsComponent implements OnInit {
  private _roomService = inject(RoomManagementService);
  private _roomMembershipService = inject(RoomMembershipService);
  private _roomJoinRequestService = inject(RoomJoinRequestService);
  private _route = inject(ActivatedRoute);
  private _snack = inject(MatSnackBar);
  private _fB = inject(FormBuilder);

  tabTypes: string[] = ['Members', 'Settings', 'Requests'];
  members: Member[] | undefined;
  tabType: string = 'Members';
  roomId: string | undefined | null;
  room: RoomResponse | undefined;
  roomType = RoomType;
  initialRoomName: string | null = null;
  initialRoomType: RoomType | null = null;
  requests: MembershipProposalsResponse[] = [];

  updateFg = this._fB.group({
    roomNameCtrl: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(30)]],
    roomType: ''
  });

  get RoomNameCtrl(): FormControl {
    return this.updateFg.get('roomNameCtrl') as FormControl;
  }

  get RoomTypeCtrl(): FormControl {
    return this.updateFg.get('roomType') as FormControl;
  }

  setTab(type: string): void {
    this.tabType = type;
    if (this.tabType === 'Settings') {
      this.setFormValues();
    } else if (this.tabType === 'Requests') {
      if (this.room && this.room.isOwner && this.room.roomType === "Private") {
        this.getAllJoinRequestComp();
      } else {
        this.requests = [];
      }
    }
  }

  getVisibleTabs(): string | string[] {
    if (this.room?.isOwner) return this.tabTypes;

    return this.tabTypes.filter(t => t === 'Members');
  }

  ngOnInit(): void {
    this.roomId = this._route.snapshot.paramMap.get('id');

    if (!this.roomId) return;

    this.getRoomByIdComp(this.roomId)
  }

  getRoomMembersComp(id: string): void {
    if (id) {
      this._roomMembershipService.getRoomMembers(id).subscribe({
        next: (res) => {
          this.members = res;
        }
      })
    }
  }

  getRoomByIdComp(id: string): void {
    if (id) {
      this._roomService.getRoomById(id).subscribe({
        next: (res) => {
          this.room = res;

          this.getRoomMembersComp(id);
        }
      })
    }
  }

  removeMemberComp(targetUserName: string, index: number): void {
    if (!this.roomId) return;

    this._roomMembershipService.removeMember(this.roomId, targetUserName).subscribe({
      next: (res) => {
        if (res) {
          this.members?.splice(index, 1);

          this._snack.open(res.message, 'Close', {
            duration: 5000,
            verticalPosition: 'top',
            horizontalPosition: 'center'
          })
        }
      }
    })
  }

  updateRoom(): void {
    if (!this.roomId) return;

    let req: UpdateRoom = {
      roomName: this.RoomNameCtrl.value,
      roomType: this.RoomTypeCtrl.value
    }

    this._roomService.updateRoom(this.roomId, req).subscribe({
      next: (res) => {
        if (res && this.room) {
          this.room.roomName = this.RoomNameCtrl.value
          this.room.roomType = this.mapEnumToString(this.RoomTypeCtrl.value);

          this.initialRoomName = this.RoomNameCtrl.value
          this.initialRoomType = this.RoomTypeCtrl.value

          this._snack.open(res.message, 'Close', {
            duration: 5000,
            verticalPosition: 'top',
            horizontalPosition: 'center'
          })
        }
      }
    })
  }

  getAllJoinRequestComp(): void {
    if (!this.roomId) return

    this._roomJoinRequestService.getAllJoinRequests(this.roomId).subscribe({
      next: (res) => {
        this.requests = res || [];
      },
      error: (err) => {
        this.requests = [];
      }
    })
  }

  approveRequestComp(requestId: string, index: number): void {
    if (!this.roomId) return;

    this._roomJoinRequestService.approveRequest(requestId).subscribe({
      next: () => {
        this.requests.splice(index, 1);
      }
    })
  }

  rejectRequestComp(requestId: string, index: number): void {
    if (!this.roomId) return;

    this._roomJoinRequestService.rejectRequest(requestId).subscribe({
      next: () => {
        this.requests.splice(index, 1);
      }
    })
  }

  setFormValues(): void {
    if (this.room) {
      this.RoomNameCtrl.setValue(this.room.roomName);

      if (this.room.roomType === "Public") {
        this.RoomTypeCtrl.setValue(RoomType.PUBLIC);
        this.initialRoomType = RoomType.PUBLIC;
      } else {
        this.RoomTypeCtrl.setValue(RoomType.PRIVATE);
        this.initialRoomType = RoomType.PRIVATE;
      }

      this.initialRoomName = this.room.roomName;
    }
  }

  isFormChanged(): boolean {
    return (
      this.RoomNameCtrl.value !== this.initialRoomName ||
      this.RoomTypeCtrl.value !== this.initialRoomType
    );
  }

  private mapEnumToString(type: RoomType): string {
    if (type === RoomType.PUBLIC) return "Public";
    if (type === RoomType.PRIVATE) return "Private";
    return "PUBLIC";
  }
}

import { Component, inject, OnInit } from '@angular/core';
import { RoomManagementService } from '../../services/room-management.service';
import { RoomMembershipService } from '../../services/room-membership.service';
import { UserService } from '../../services/user.service';
import { User } from '../../models/user.mode';
import { CommonModule } from '@angular/common';
import { RoomResponse } from '../../models/room.model';
import { RoomCardComponent } from "../rooms/room-card/room-card.component";
import { RouterLink, RouterModule } from '@angular/router';

@Component({
  selector: 'app-profile',
  imports: [CommonModule, RoomCardComponent, RouterLink, RouterModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  private _roomManagementService = inject(RoomManagementService);
  private _roomMembershipService = inject(RoomMembershipService);
  private _userService = inject(UserService);

  user: User | undefined;
  userRooms: RoomResponse[] = [];
  joinedRooms: RoomResponse[] = [];
  tabTypes: string[] = ['My Rooms', 'Joined Rooms'];
  tabType: string = 'My Rooms';

  ngOnInit(): void {
    this.getUserByIdComp();

    this.getUserRoomsComp();
  }

  setTabType(type: string): void {
    this.tabType = type;

    if (this.tabType === 'My Rooms')
      this.getUserRoomsComp();
    else 
      this.getRoomUserIsMemberOfComp()
  }

  getUserByIdComp(): void {
    this._userService.getUserById().subscribe({
      next: (res) => {
        this.user = res;
      }
    })
  }

  getUserRoomsComp(): void {
    this._roomManagementService.getRoomsCreatedByUser().subscribe({
      next: (res) => {
        this.userRooms = res ?? [];
      }
    })
  }

  getRoomUserIsMemberOfComp(): void {
    this._roomMembershipService.getRoomUserIsMemberOf().subscribe({
      next: (res) => {
        this.joinedRooms = res ?? [];
      }
    })
  }

  removeLeavedRoomFromRooms(roomId: string): void {
    const rooms = this.joinedRooms.filter(room => room.id !== roomId);
    this.joinedRooms = rooms;
  }
}

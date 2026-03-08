import { Component, inject, signal } from '@angular/core';
import { RoomManagementService } from '../../../services/room-management.service';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateRoom } from '../../../models/create-room.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-create-room',
  imports: [FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './create-room.component.html',
  styleUrl: './create-room.component.scss'
})
export class CreateRoomComponent {
  private _roomService = inject(RoomManagementService);
  private _router = inject(Router);
  private _snack = inject(MatSnackBar);
  private _fB = inject(FormBuilder);

  isLoadingSig = signal<boolean>(false);

  createFg = this._fB.group({
    roomNameCtrl: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(30)]],
    roomType: 'Public'
  });

  get RoomNameCtrl(): FormControl {
    return this.createFg.get('roomNameCtrl') as FormControl;
  }

  get RoomTypeCtrl(): FormControl {
    return this.createFg.get('roomType') as FormControl;
  }

  createRoomComp(): void {
    this.isLoadingSig.set(true);

    let req: CreateRoom = {
      roomName: this.RoomNameCtrl.value,
      roomType: this.RoomTypeCtrl.value
    }

    this._roomService.createRoom(req).subscribe({
      next: (res) => {
        this._snack.open(res.message, 'Close', {
          duration: 5000,
          verticalPosition: 'top',
          horizontalPosition: 'center'
        });

        this._router.navigateByUrl('/dashboard');

        this.isLoadingSig.set(false);
      },
      error: () => {
        this.isLoadingSig.set(false);
      }
    })
  }
}

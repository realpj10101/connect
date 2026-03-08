import { ChangeDetectorRef, Component, inject, OnInit, signal } from '@angular/core';
import { NavbarComponent } from "../navbar/navbar.component";
import { RoomResponse } from '../../models/room.model';
import { PaginationParams } from '../../models/helpers/paginationParams.model';
import { RoomManagementService } from '../../services/room-management.service';
import { PaginatedResult } from '../../models/helpers/paginatedResult';
import { Pagination } from '../../models/helpers/pagination';
import { RoomCardComponent } from "../rooms/room-card/room-card.component";
import { RoomParams } from '../../models/helpers/roomParams.model';
import { FormBuilder, FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { OrderByEnum } from '../../enums/order-by-enum';
import { debounceTime } from 'rxjs';
import { Router, RouterLink, RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  imports: [RoomCardComponent, ReactiveFormsModule, FormsModule,],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private _roomService = inject(RoomManagementService);
  private _fb = inject(FormBuilder);

  isLoading = signal<boolean>(false);

  roomType: string = 'All';
  roomTypes: string[] = ['All', 'Public', 'Private'];
  rooms: RoomResponse[] | undefined;
  roomParams: RoomParams | undefined;
  pagination: Pagination | undefined;

  filterFg = this._fb.group({
    searchCtrl: [''],
    orderByCtrl: [OrderByEnum.ALL]
  })

  get SearchCtrl(): FormControl {
    return this.filterFg.get('searchCtrl') as FormControl;
  }

  get OrderByCtrl(): FormControl {
    return this.filterFg.get('orderByCtrl') as FormControl;
  }

  ngOnInit(): void {
    this.roomParams = new RoomParams();

    this.getAll();

    this.SearchCtrl.valueChanges
      .pipe(
        debounceTime(400)
      )
      .subscribe(() => {
        this.updateRoomParams();
        this.getAll();
      })
  }

  goToNextPage(): void {
    if (this.roomParams && this.pagination && this.pagination.currentPage < this.pagination.totalPages) {
      this.roomParams.pageNumber++;
      this.getAll();
    }
  }

  goToPrevPage(): void {
    if (this.roomParams && this.pagination && this.pagination.currentPage > 1) {
      this.roomParams.pageNumber--;
      this.getAll();
    }
  }

  getPageNumbers(): number[] {
    if (!this.pagination) return [];

    const total = this.pagination.totalPages;
    const current = this.pagination.currentPage;

    const pages: number[] = [];

    const start = Math.max(1, current - 2);
    const end = Math.min(total, current + 2);

    for (let i = start; i <= end; i++) {
      pages.push(i);
    }

    return pages;
  }

  goToPage(page: number): void {
    if (this.roomParams && this.pagination && page !== this.pagination.currentPage) {
      this.roomParams.pageNumber = page;
      this.getAll();
    }
  }

  setRoomType(type: string): void {
    switch (type) {
      case 'All':
        this.OrderByCtrl.setValue(OrderByEnum.ALL);
        break;
      case 'Public':
        this.OrderByCtrl.setValue(OrderByEnum.PUBLIC);
        break;
      case 'Private':
        this.OrderByCtrl.setValue(OrderByEnum.PRIVATE);
        break;
    }

    this.roomType = type;

    this.updateRoomParams();
    this.getAll();
  }

  getAll(): void {
    if (this.roomParams) {
      this.isLoading.set(true);

      this._roomService.getAll(this.roomParams).subscribe({
        next: (res: PaginatedResult<RoomResponse[]>) => {
          this.isLoading.set(false);

          this.rooms = res.body ?? [];
          this.pagination = res.pagination;
        },
        error: () => {
          this.isLoading.set(false);
        }
      })
    }
  }

  updateRoomParams(): void {
    if (this.roomParams) {
      this.roomParams.orderBy = this.OrderByCtrl.value;
      this.roomParams.search = this.SearchCtrl.value;
    }
  }
}

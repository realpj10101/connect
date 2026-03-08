import { TestBed } from '@angular/core/testing';

import { RoomJoinRequestService } from './room-join-request.service';

describe('RoomJoinRequestService', () => {
  let service: RoomJoinRequestService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(RoomJoinRequestService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

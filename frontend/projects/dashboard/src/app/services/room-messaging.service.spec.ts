import { TestBed } from '@angular/core/testing';

import { RoomMessagingService } from './room-messaging.service';

describe('RoomMessagingService', () => {
  let service: RoomMessagingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(RoomMessagingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';

import { RoomMessageService } from './room-message.service';

describe('RoomMessageService', () => {
  let service: RoomMessageService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(RoomMessageService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

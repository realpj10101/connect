import { TestBed } from '@angular/core/testing';

import { RoomMembershipService } from './room-membership.service';

describe('RoomMembershipService', () => {
  let service: RoomMembershipService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(RoomMembershipService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});

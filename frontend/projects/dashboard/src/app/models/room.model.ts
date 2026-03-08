export interface RoomResponse {
    id: string;
    ownerName: string;
    roomName: string;
    memberCount: number;
    roomType: string;
    createdAt: Date;
    isMember: boolean;
    hasPendingRequest: boolean;
    isOwner: boolean;
}
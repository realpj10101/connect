import { RoomType } from "../enums/room-type-enum";

export interface UpdateRoom {
    roomName: string;
    roomType: RoomType;
}
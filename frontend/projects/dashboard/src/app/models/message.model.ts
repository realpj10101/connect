import { ChatItem } from "./chat-item.model";

export interface MessageReq {
    message: string;
}

export interface MessageRes {
    id: string;
    message: string;
    senderUserName: string;
    timeStamp: string;
}

export interface MessagePage {
    messages: ChatItem[];
    hasMore: boolean;
}
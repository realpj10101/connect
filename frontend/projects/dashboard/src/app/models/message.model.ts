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
    messages: MessageRes[];
    hasMore: boolean;
}
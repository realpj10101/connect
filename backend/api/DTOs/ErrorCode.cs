namespace api.DTOs;

public enum ErrorCode
{
    WrongCreds,
    NetIdentityFailed,
    UserNotFound,
    RoomNotFound,
    NotInRoom,
    AlreadyInRoom,
    AlreadySentRequest,
    PrivateRoom,
    PublicRoom,
    JoinToOwnRoom,
    LeftOwnRoom,
    NotRoomOwner,
    NotRoomMember,
    SaveToDiskFailed,
    AudioNotFound,
    NoMemberInRoom,
    MembershipPropNotExist,
    AlreadyAccepted,
    AlreadyRejected,
    Failed,
    InvalidType,
    RoomAlreadyExist,
    TransactionFailed,
    AssignRoleFailed,
    UserAlreadyExist,
    UpdateFailed,
    InvalidData
}
using api.DTOs;
using api.DTOs.Helpers;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IRoomMembershipRepository
{
    public Task<OperationResult> JoinRoomAsync(ObjectId roomId, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<OperationResult> LeaveRoomAsync(ObjectId roomId, ObjectId userId, CancellationToken cancellationToken);
    
    public Task<OperationResult> RemoveMemberAsync(ObjectId roomId, ObjectId userId, string targetUserName,
        CancellationToken cancellationToken);
    
    public Task<OperationResult<IEnumerable<MemberDto>>> GetRoomMembersAsync(ObjectId roomId,
        CancellationToken cancellationToken);
    
    public Task<bool> CheckIsMemberAsync(ObjectId userId, ObjectId roomId, CancellationToken cancellationToken);
    
    public Task<bool> CheckMembershipAsync(ObjectId roomId, ObjectId userId, CancellationToken cancellationToken);

    public Task<OperationResult<IEnumerable<RoomResponse>>> GetRoomsUserIsMemberOfAsync(ObjectId userId, CancellationToken cancellationToken);
}
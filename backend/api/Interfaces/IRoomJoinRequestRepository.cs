using api.DTOs;
using api.DTOs.Helpers;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IRoomJoinRequestRepository
{
    public Task<OperationResult>
        JoinRequestAsync(ObjectId roomId, ObjectId userId, CancellationToken cancellationToken);

    public Task<OperationResult> ApproveRequestAsync(ObjectId joinRequestId, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<OperationResult> RejectRequestAsync(ObjectId joinRequestId, ObjectId userId,
        CancellationToken cancellationToken);

    public Task<bool> CheckHasPendingRequestsAsync(ObjectId userId, ObjectId roomId,
        CancellationToken cancellationToken);

    public Task<OperationResult<IEnumerable<MembershipProposalResponse>>> GetAllJoinRequestsAsync(ObjectId roomId,
        ObjectId userId, CancellationToken cancellationToken);
}
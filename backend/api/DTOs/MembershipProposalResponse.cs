using api.Enums;
using MongoDB.Bson;

namespace api.DTOs;

public record MembershipProposalResponse(
    string Id,
    string SenderName,
    JoinStatus Status,
    DateTime CreatedAt
);
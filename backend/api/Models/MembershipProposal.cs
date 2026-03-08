using api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class MembershipProposal
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? Id { get; init; }

    public ObjectId RoomId { get; init; }
    public ObjectId UserId { get; init; }
    public JoinStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
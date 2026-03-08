using api.DTOs;
using api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class Room
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? Id { get; init; }
    public ObjectId OwnerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public RoomType RoomType { get; init; }
    public List<ObjectId> MemberIds { get; init; } = default!;
    public int MembersCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
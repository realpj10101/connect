using api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class RoomMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? Id { get; init; }
    public ObjectId RoomId { get; init; }
    public ObjectId SenderId { get; init; }
    public ChatItemType Type { get; init; }
    public string? Message { get; init; }
    public string? FilePath { get; init; }
    public string? FileName { get; init; }
    public long? FileSize { get; init; }
    public int? Duration { get; init; }
    public string? MimeType { get; init; } = string.Empty;
    public Photo? Photo { get; init; }
    public DateTime TimeStamp { get; init; }
}
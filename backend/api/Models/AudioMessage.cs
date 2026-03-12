using api.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class AudioMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? Id { get; init; }

    public ObjectId RoomId { get; init; }
    public ObjectId UploaderId { get; init; }
    public AudioType Type { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public int Duration { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
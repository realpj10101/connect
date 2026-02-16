using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class RoomChat
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId? Id { get; init; }
    public ObjectId RoomId { get; init; }
    public ObjectId SenderId { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTime TimeStamp { get; init; }
}
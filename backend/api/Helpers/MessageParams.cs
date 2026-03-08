using MongoDB.Bson;

namespace api.Helpers;

public class MessageParams
{
    public int Limit { get; set; } = 20;
    public ObjectId? LastMessageId { get; set; }
}
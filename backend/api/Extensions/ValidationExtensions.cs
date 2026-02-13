using MongoDB.Bson;

namespace api.Extensions;

public static class ValidationExtensions
{
    public static ObjectId? ValidateObjectId(ObjectId? objectId)
    {
        return objectId is null || !objectId.HasValue || objectId.Equals(ObjectId.Empty)
            ? null
            : objectId;
    }
}
using MongoDB.Bson;

namespace api.Interfaces;

public interface IVideoService
{
    public Task<string?> SaveVideoToDiskAsync(IFormFile? videoFile, ObjectId userId, ObjectId videoId);
}
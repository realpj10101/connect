using MongoDB.Bson;

namespace api.Interfaces;

public interface IAudioService
{
    public Task<string?> SaveAudioToDiskAsync(IFormFile? audioFile, ObjectId userId, ObjectId audioId);
    public bool DeleteAudioFromDisk(string relativePath);  
}
using api.Models;
using MongoDB.Bson;

namespace api.Interfaces;

public interface IPhotoService
{
    public Task<string[]?> AddPhotoToDiskAsync(IFormFile file, ObjectId photoId);

    public Task<bool> DeletePhotoFromDisk(Photo photo);
}
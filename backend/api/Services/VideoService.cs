using api.Interfaces;
using MongoDB.Bson;

namespace api.Services;

public class VideoService : IVideoService
{
    private const string WwwRootPath = "wwwroot/";
    private const string VideoRoot = "storage/videos/";

    public async Task<string?> SaveVideoToDiskAsync(IFormFile? videoFile, ObjectId userId, ObjectId videoId)
    {
        if (videoFile is null || videoFile.Length == 0)
            return null;

        string userFolder = Path.Combine(VideoRoot, userId.ToString(), "original");
        string absolutePath = Path.Combine(WwwRootPath, userFolder);

        Directory.CreateDirectory(absolutePath);

        string originalName = Path.GetFileNameWithoutExtension(videoFile.FileName);
        string extension = Path.GetExtension(videoFile.FileName);

        string fileName = $"{videoId}_{originalName}{extension}";
        string filePath = Path.Combine(absolutePath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await videoFile.CopyToAsync(stream);

        return Path.Combine(userFolder, fileName).Replace("\\", "/");
    }
}
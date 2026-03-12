using api.Interfaces;
using MongoDB.Bson;

namespace api.Services;

public class AudioService : IAudioService
{
    private const string WwwRootPath = "wwwroot/";
    private const string AudioRoot = "storage/audios/";

    public async Task<string?> SaveAudioToDiskAsync(IFormFile? audioFile, ObjectId userId, ObjectId audioId)
    {
        if (audioFile is null || audioFile.Length == 0)
            return null;

        string userFolder = Path.Combine(AudioRoot, userId.ToString(), "original");
        string absolutePath = Path.Combine(WwwRootPath, userFolder);
        Directory.CreateDirectory(absolutePath);

        string originalName = Path.GetFileNameWithoutExtension(audioFile.FileName);
        string extension = Path.GetExtension(audioFile.FileName);

        string fileName = $"{audioId}_{originalName}{extension}";
        string filePath = Path.Combine(absolutePath, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await audioFile.CopyToAsync(stream);

        return Path.Combine(userFolder, fileName).Replace("\\", "/");

    }

    public bool DeleteAudioFromDisk(string relativePath)
    {
        throw new NotImplementedException();
    }
}
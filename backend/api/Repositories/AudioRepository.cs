using api.DTOs;
using api.DTOs.Helpers;
using api.Enums;
using api.Extensions;
using api.Interfaces;
using api.Models;
using api.Settings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace api.Repositories;

public class AudioRepository : IAudioRepository
{
    #region Variables and Constructor

    private readonly IMongoCollection<Room> _collectionRooms;
    private readonly IMongoCollection<AudioMessage> _collectionAudios;
    private readonly IAudioService _audioService;

    public AudioRepository(IMongoClient client, IMyMongoDbSettings dbSettings, IAudioService audioService)
    {
        var database = client.GetDatabase(dbSettings.DatabaseName);
        _collectionRooms = database.GetCollection<Room>(AppVariablesExtensions.CollectionRooms);
        _collectionAudios = database.GetCollection<AudioMessage>(AppVariablesExtensions.CollectionAudios);

        _audioService = audioService;
    }

    #endregion

    public async Task<OperationResult<AudioMessage>> UploadVoiceAsync(IFormFile voiceFile, ObjectId roomId,
        ObjectId userId, CancellationToken cancellationToken)
    {
        return await ImplementUploadAudioAsync(voiceFile, roomId, userId, AudioType.Voice, cancellationToken);
    }

    public async Task<OperationResult<AudioMessage>> UploadAudioAsync(IFormFile audioFile, ObjectId roomId, ObjectId userId, CancellationToken cancellationToken)
    {
        return await ImplementUploadAudioAsync(audioFile, roomId, userId, AudioType.Audio, cancellationToken);
    }

    public async Task<OperationResult<AudioMessage>> GetAudioByIdAsync(ObjectId audioId, ObjectId userId,
        CancellationToken cancellationToken)
    {
        AudioMessage targetAudio =
            await _collectionAudios.Find(doc => doc.Id == audioId).FirstOrDefaultAsync(cancellationToken);

        if (targetAudio is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.AudioNotFound,
                    "Audio not found."
                )
            );
        }

        Room? targetRoom = await _collectionRooms.Find(doc => doc.Id == targetAudio.RoomId).FirstOrDefaultAsync(cancellationToken);

        if (targetRoom is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.RoomNotFound,
                    "Room not found."
                )
            );
        }

        if (targetRoom.RoomType is RoomType.Private && !targetRoom.MemberIds.Contains(userId) &&
            targetRoom.OwnerId != userId)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.NotRoomMember,
                    "You are not the member of this room. Please join this room first."
                )
            );
        }

        return new(
            true,
            targetAudio,
            null
        );
    }

    private async Task<OperationResult<AudioMessage>> ImplementUploadAudioAsync(IFormFile file, ObjectId roomId,
        ObjectId userId, AudioType audioType, CancellationToken cancellationToken)
    {
          Room? targetRoom = await _collectionRooms.Find(doc => doc.Id == roomId).FirstOrDefaultAsync(cancellationToken);

        if (targetRoom is null)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.RoomNotFound,
                    "Room not found."
                )
            );
        }

        if (targetRoom.RoomType is RoomType.Private && !targetRoom.MemberIds.Contains(userId) &&
            targetRoom.OwnerId != userId)
        {
            return new(
                false,
                Error: new CustomError(
                    ErrorCode.NotRoomMember,
                    "You are not the member of this room. Please join this room first."
                )
            );
        }
        
        ObjectId audioId = ObjectId.GenerateNewId();

        string? filePath = await _audioService.SaveAudioToDiskAsync(file, userId, audioId);

        if (filePath is null)
        {
            return new(
                false,
                Error: new(
                    ErrorCode.SaveToDiskFailed,
                    "Save audio to disk failed."
                )
            );
        }

        TimeSpan duration = TimeSpan.Zero;
        try
        {
            string physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath);
            using var tFile = TagLib.File.Create(physicalPath);
            duration = tFile.Properties.Duration;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Metadata Error: {ex.Message}");
        }

        AudioMessage audioMessage = new AudioMessage
        {
            Id = audioId,
            RoomId = roomId,
            UploaderId = userId,
            Type = audioType,
            FilePath = filePath,
            FileName = file.FileName,
            FileSize = file.Length,
            MimeType = file.ContentType,
            Duration = (int)duration.TotalMilliseconds,
            CreatedAt = DateTime.UtcNow
        };

        await _collectionAudios.InsertOneAsync(audioMessage, null, cancellationToken);

        return new(
            true,
            audioMessage,
            null
        ); 
    }
}
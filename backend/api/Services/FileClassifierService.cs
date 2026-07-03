using api.Enums;
using api.Interfaces;

namespace api.Services;

public class FileClassifierService : IFileClassifierService
{
    public ChatItemType DetectFileType(IFormFile? file)
    {
        if (file is null || file.Length < 12)
            return ChatItemType.Unknown;

        return DetectByMagicBytes(file);
    }

    private ChatItemType DetectByMagicBytes(IFormFile? file)
    {
        Span<byte> header = stackalloc byte[16];

        using (var stream = file.OpenReadStream())
        {
            stream.Read(header);
        }

        // ================== IMAGE ==================

        // PNG
        if (header[..4].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }))
            return ChatItemType.Image;

        // JPEG
        if (header[0] == 0xFF && header[1] == 0xD8)
            return ChatItemType.Image;

        // GIF
        if (header[..3].SequenceEqual(new byte[] { 0x47, 0x49, 0x46 }))
            return ChatItemType.Image;

        // BMP
        if (header[..2].SequenceEqual(new byte[] { 0x42, 0x4D }))
            return ChatItemType.Image;

        // WEBP → RIFF....WEBP
        if (header[0] == 0x52 && header[1] == 0x49 &&
            header[2] == 0x46 && header[3] == 0x46 &&
            header[8] == 0x57 && header[9] == 0x45 &&
            header[10] == 0x42 && header[11] == 0x50)
            return ChatItemType.Image;

        // ---------- VIDEO MAGIC BYTES ----------
        // MP4 / MOV / 3GP → ....ftyp
        if (header[4] == 0x66 &&
            header[5] == 0x74 &&
            header[6] == 0x79 &&
            header[7] == 0x70)
            return ChatItemType.Video;

        // MKV / WEBM
        if (header[0] == 0x1A &&
            header[1] == 0x45 &&
            header[2] == 0xDF &&
            header[3] == 0xA3)
            return ChatItemType.Video;

        // AVI → RIFF....AVI
        if (header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x41 &&
            header[9] == 0x56 &&
            header[10] == 0x49)
            return ChatItemType.Video;

        // FLV
        if (header[0] == 0x46 &&
            header[1] == 0x4C &&
            header[2] == 0x56)
            return ChatItemType.Video;

        // WMV / ASF
        if (header[0] == 0x30 &&
            header[1] == 0x26 &&
            header[2] == 0xB2 &&
            header[3] == 0x75)
            return ChatItemType.Video;

        return ChatItemType.Unknown;
    }
}
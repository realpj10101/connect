using System.ComponentModel.DataAnnotations;

namespace api.Validations;

public class AllowedMediaFileExtensionsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var file = value as IFormFile;
        if (file is not null)
        {
            if (!IsFileValid(file))
            {
                string? keys = null;
                foreach (var key in _fileSignatures.Keys)
                {
                    keys += key + ", ";
                }

                return new ValidationResult($"File type is not allowed. These extensions are allowed only: {keys}");
            }
        }

        return ValidationResult.Success;
    }

    public static bool IsFileValid(IFormFile file)
    {
        using var reader = new BinaryReader(file.OpenReadStream());
        var signatures = _fileSignatures.Values.SelectMany(x => x).ToList();
        var headerBytes = reader.ReadBytes(_fileSignatures.Max(m => m.Value.Max(n => n.Length)));

        bool result = signatures.Any(signature =>
            headerBytes.Take(signature.Length).SequenceEqual(signature));

        return result;
    }

    public static Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        // ================= IMAGE =================

        {
            ".jpg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, // JFIF
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 }, // EXIF
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
            }
        },

        {
            ".jpeg", new List<byte[]>
            {
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
                new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
            }
        },

        {
            ".png", new List<byte[]>
            {
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            }
        },

        {
            ".gif", new List<byte[]>
            {
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, // GIF87a
                new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 } // GIF89a
            }
        },

        {
            ".bmp", new List<byte[]>
            {
                new byte[] { 0x42, 0x4D }
            }
        },

        {
            ".webp", new List<byte[]>
            {
                new byte[] { 0x52, 0x49, 0x46, 0x46 } // RIFF .... WEBP
            }
        },


        // ================= VIDEO =================

        // MP4 (ISO Base Media file format)
        {
            ".mp4", new List<byte[]>
            {
                new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, // ftyp
                new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 },
            }
        },

        // MOV (QuickTime)
        {
            ".mov", new List<byte[]>
            {
                new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 },
            }
        },

        // AVI
        {
            ".avi", new List<byte[]>
            {
                new byte[] { 0x52, 0x49, 0x46, 0x46 }, // "RIFF"
            }
        },

        // MKV / WebM (Matroska)
        {
            ".mkv", new List<byte[]>
            {
                new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, // Matroska header
            }
        },
        {
            ".webm", new List<byte[]>
            {
                new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, 
            }
        },

        // FLV (Flash Video)
        {
            ".flv", new List<byte[]>
            {
                new byte[] { 0x46, 0x4C, 0x56, 0x01 }, // "FLV" + version
            }
        },

        // WMV (ASF container)
        {
            ".wmv", new List<byte[]>
            {
                new byte[] { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11 },
            }
        },

        // 3GP (mobile video format, based on ISO Base Media)
        {
            ".3gp", new List<byte[]>
            {
                new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 },
            }
        },
    };
}
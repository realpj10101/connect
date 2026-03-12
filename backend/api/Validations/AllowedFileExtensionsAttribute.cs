using System.ComponentModel.DataAnnotations;

namespace api.Validations;

public class AllowedFileExtensionsAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var file = value as IFormFile;
        if (file is not null)
        {
            if (!IsFileValid(file))
            {
                // get only allowed extensions to show
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
        var signatures = _fileSignatures.Values.SelectMany(x => x).ToList();  // flatten all signatures to single list
        var headerBytes = reader.ReadBytes(_fileSignatures.Max(m => m.Value.Max(n => n.Length)));
        bool result = signatures.Any(signature => headerBytes.Take(signature.Length).SequenceEqual(signature));
        return result;
    }

    public static Dictionary<string, List<byte[]>> _fileSignatures = new()
    {
        { ".mp3", new List<byte[]>
            {
                new byte[] { 0x49, 0x44, 0x33 },     // ID3 tag
                new byte[] { 0xFF, 0xFB },
                new byte[] { 0xFF, 0xF3 },
                new byte[] { 0xFF, 0xF2 }
            }
        },
        { ".wav", new List<byte[]>
            {
                new byte[] { 0x52, 0x49, 0x46, 0x46 } // RIFF
            }
        },
        { ".ogg", new List<byte[]>
            {
                new byte[] { 0x4F, 0x67, 0x67, 0x53 } // OggS
            }
        },
        { ".opus", new List<byte[]>
            {
                new byte[] { 0x4F, 0x67, 0x67, 0x53 } // Opus inside OGG
            }
        },
        { ".m4a", new List<byte[]>
            {
                new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 },
                new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }
            }
        },
        { ".aac", new List<byte[]>
            {
                new byte[] { 0xFF, 0xF1 },
                new byte[] { 0xFF, 0xF9 }
            }
        },
        { ".webm", new List<byte[]>
            {
                new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } // EBML
            }
        },
    };
}
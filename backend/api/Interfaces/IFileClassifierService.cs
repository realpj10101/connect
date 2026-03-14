using api.Enums;

namespace api.Interfaces;

public interface IFileClassifierService
{
    public ChatItemType DetectFileType(IFormFile file);
}
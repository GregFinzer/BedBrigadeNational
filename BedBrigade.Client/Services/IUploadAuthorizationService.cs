namespace BedBrigade.Client.Services;

public interface IUploadAuthorizationService
{
    string CreateImageUploadToken(int locationId, string contentType, string contentName);
    bool TryValidateImageUploadToken(string token, int locationId, string contentType, string contentName,
        out string errorMessage);
}


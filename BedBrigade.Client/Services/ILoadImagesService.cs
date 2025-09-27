namespace BedBrigade.Client.Services;

public interface ILoadImagesService
{
    /// <summary>
    /// Get a rotated image for the path and area
    /// </summary>
    /// <param name="path">Path for the location</param>
    /// <param name="area">Id of the image rotator</param>
    /// <returns></returns>
    /// <example>
    /// path = "national\pages\home"
    /// area = "headerImageRotator
    /// </example>
    string GetRotatedImage(string path, string area);

    /// <summary>
    /// Sets the rotated images for the html
    /// </summary>
    /// <param name="path">Path for the location</param>
    /// <param name="originalHtml"></param>
    /// <example>
    /// path = "national\pages\home"
    /// </example>
    /// <returns></returns>
    string SetImagesForHtml(string path, string originalHtml);

    /// <summary>
    /// Gets all the images for a given path
    /// </summary>
    /// <param name="path">Path for the location</param>
    /// <param name="area">Id of the image rotator</param>
    /// <returns></returns>
    /// <example>
    /// path = "national\pages\home"
    /// area = "headerImageRotator
    /// </example>
    List<string> GetImagesForArea(string path, string area);

    List<string> GetImgIdsWithRotator(string html);

    void EnsureDirectoriesExist(string path, string html);

    string SetImgSourceForImageRotators(string path, string html);

    List<string> GetImagesForLocationWithDefault(string path, string area);

    string GetRotatedImage(List<string> images);
    string GetDirectoryForPathAndArea(string path, string area);

    Task<string> ConvertToWebp(string targetPath);
}
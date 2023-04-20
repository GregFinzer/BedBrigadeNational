
using static BedBrigade.Common.Common;

namespace BedBrigade.Common;


public class BedRequestEnumItem
{
    public BedRequestStatus Value { get; set; }
    public string? Name { get; set; }

}

public class ConfigSectionEnumItem
{
    public ConfigSection Value { get; set; }
    public string? Name { get; set; }

}

public class FileUseEnumItem
{
    public FileUse Value { get; set; }
    public string? Name { get; set; }

}

public static class Common
{
    public enum PersistGrid
    {
        Configuration = 1,
        User = 2,
        Location = 3,
        Volunteer = 4,
        Donation = 5,
        Content = 6,
        BedRequest = 7,
        Media = 8,
        Pages = 9

    }

    public enum BedRequestStatus
    {
       Requested = 1,
       Scheduled = 2,
       Delivered = 3,
    }

    /// <summary>
    /// Get a list of Enum Items suitable for a dropdown list from the BedRequestStatusEnum
    /// </summary>
    /// <returns>List<EnumItem></EnumItem></returns>
    public static List<BedRequestEnumItem> GetBedRequestStatusItems()
    {
        var type = typeof(BedRequestStatus);
        return Enum.GetValues(type).OfType<BedRequestStatus>().ToList()
                        .Select(x => new BedRequestEnumItem
                        {
                            Value = (BedRequestStatus)x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
    }

    public enum FileUse
    {
        Unknown = 0,
        Logo = 1, // an image used as a logo
        Image = 2, // an image used as an image
        Download = 3, // a downloadable file (pdf,csv,etc)
        Text = 4, // a text file
        Html = 5, // Raw Html
        Icon = 6
    }

    /// <summary>
    /// Get a list of Enum Items suitable for a dropdown list from the ConfigSection enum.
    /// </summary>
    /// <returns>List<EnumItem></EnumItem></returns>
    public static List<FileUseEnumItem> GetFileUseItems()
    {
        var type = typeof(FileUse);
        return Enum.GetValues(type).OfType<FileUse>().ToList()
                        .Select(x => new FileUseEnumItem
                        {
                            Value = (FileUse)x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
    }


    public enum ConfigSection
    {
        System = 1,
        Media = 2,
        Email = 3
    }


    /// <summary>
    /// Get a list of Enum Items suitable for a dropdown list from the ConfigSection enum.
    /// </summary>
    /// <returns>List<EnumItem></EnumItem></returns>
    public static List<ConfigSectionEnumItem> GetConfigSectionItems()
    {
        var type = typeof(ConfigSection);
        return Enum.GetValues(type).OfType<ConfigSection>().ToList()
                        .Select(x => new ConfigSectionEnumItem
                        {
                            Value = (ConfigSection) x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
    }
}

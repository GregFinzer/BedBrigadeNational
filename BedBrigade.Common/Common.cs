
using static BedBrigade.Common.Common;

namespace BedBrigade.Common;

public class ContentTypeEnumItem
{
    public ContentType Value { get; set; }
    public string? Name { get; set; }

}

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
    public enum ContentType
    {
        Header = 1,
        Footer = 2,
        Body = 3,
    }

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
    public static List<ContentTypeEnumItem> GetContentTypeItems()
    {
        var type = typeof(ContentType);
        return Enum.GetValues(type).OfType<ContentType>().ToList()
                        .Select(x => new ContentTypeEnumItem
                        {
                            Value = (ContentType)x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
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

    public static void CreateDirectory(string targetDir)
    {
        Directory.CreateDirectory(targetDir);
    }


    public static void DeleteDirectory(string targetDir)
    {
        File.SetAttributes(targetDir, FileAttributes.Normal);

        string[] files = Directory.GetFiles(targetDir);
        string[] dirs = Directory.GetDirectories(targetDir);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(targetDir, false);
    }

    public static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyAll(diSource, diTarget);
    }

    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            Console.WriteLine($"Create sub dir {diSourceSubDir.Name}");
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

}

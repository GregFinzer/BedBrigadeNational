using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;

namespace BedBrigade.Common.Logic;

// ENUM & other classes

//=======================================================================
public static class Common
{




    /// <summary>
    /// Get a list of Enum Items suitable for a dropdown list from the EventTypeEnumItems
    /// </summary>
    /// <returns>List<EnumItem></EnumItem></returns>
    public static List<EventTypeEnumItem> GetEventTypeItems() // added by VS 7/1/2023
    {
        var type = typeof(EventType);
        return Enum.GetValues(type).OfType<EventType>().ToList()
                        .Select(x => new EventTypeEnumItem
                        {
                            Value = x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
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
                            Value = x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
    }

    public static List<VehicleTypeEnumItem> GetVehicleTypeItems()
    {
        var type = typeof(VehicleType);
        return Enum.GetValues(type).OfType<VehicleType>().ToList()
                        .Select(x => new VehicleTypeEnumItem
                        {
                            Value = x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
    }


    public static List<EventStatusEnumItem> GetEventStatusItems() // added by VS 7/1/2023
    {
        var type = typeof(EventStatus);
        return Enum.GetValues(type).OfType<EventStatus>().ToList()
                        .Select(x => new EventStatusEnumItem
                        {
                            Value = x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
    } // Get Event Status Items


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
                            Value = x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
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
                            Value = x,
                            Name = Enum.GetName(type, x)
                        })
                        .ToList();
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
                            Value = x,
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

    public static bool IsDevelopment()
    {
        string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment == "Development";
    }

    public static string GetHtml(string fileName)
    {
        string filePath = $"{GetSeedingDirectory()}/SeedHtml/{fileName}";

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var html = File.ReadAllText(filePath);
        return html;
    }

    public static string GetSeedingDirectory()
    {
        string localFilePath = "../BedBrigade.Data/Data/Seeding";

        if (Directory.Exists(localFilePath))
        {
            return localFilePath;
        }

        Console.WriteLine("Directory does not exist: " + localFilePath);

        string deployedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Seeding");

        if (Directory.Exists(deployedFilePath))
        {
            return deployedFilePath;
        }

        Console.WriteLine("Directory does not exist: " + deployedFilePath);

        throw new DirectoryNotFoundException("Seeding directory not found. Current directory is : " + AppDomain.CurrentDomain.BaseDirectory);
    }

    public static List<UsState> GetStateList()
    {
        List<UsState>? StateList = new List<UsState>
        {
            new UsState {StateCode = "AL", StateName = "Alabama",ZipCodeMin = 35004, ZipCodeMax = 36925},
            new UsState {StateCode = "AK", StateName = "Alaska",ZipCodeMin = 99501,ZipCodeMax = 99950},
            new UsState {StateCode = "AZ",StateName = "Arizona",ZipCodeMin = 85001,ZipCodeMax = 86556},
            new UsState {StateCode = "AR",StateName = "Arkansas",ZipCodeMin = 71601,ZipCodeMax = 72959},
            new UsState {StateCode = "CA",StateName = "California",ZipCodeMin = 90001,ZipCodeMax = 96162},
            new UsState {StateCode = "CO",StateName = "Colorado",ZipCodeMin = 80001,ZipCodeMax = 81658},
            new UsState {StateCode = "CT",StateName = "Connecticut",ZipCodeMin = 6001,ZipCodeMax = 6928},
            new UsState {StateCode = "DE",StateName = "Delaware",ZipCodeMin = 19701,ZipCodeMax = 19980},
            new UsState {StateCode = "DC",StateName = "District of Columbia",ZipCodeMin = 20001,ZipCodeMax = 20799},
            new UsState {StateCode = "FL",StateName = "Florida",ZipCodeMin = 32003,ZipCodeMax = 34997},
            new UsState {StateCode = "GA",StateName = "Georgia",ZipCodeMin = 30002,ZipCodeMax = 39901},
            new UsState {StateCode = "HI",StateName = "Hawaii",ZipCodeMin = 96701,ZipCodeMax = 96898},
            new UsState {StateCode = "ID",StateName = "Idaho",ZipCodeMin = 83201,ZipCodeMax = 83877},
            new UsState {StateCode = "IL",StateName = "Illinois",ZipCodeMin = 60001,ZipCodeMax = 62999},
            new UsState {StateCode = "IN",StateName = "Indiana",ZipCodeMin = 46001,ZipCodeMax = 47997},
            new UsState {StateCode = "IA",StateName = "Iowa",ZipCodeMin = 50001,ZipCodeMax = 52809},
            new UsState {StateCode = "KS",StateName = "Kansas",ZipCodeMin = 66002,ZipCodeMax = 67954},
            new UsState {StateCode = "KY",StateName = "Kentucky",ZipCodeMin = 40003,ZipCodeMax = 42788},
            new UsState {StateCode = "LA",StateName = "Louisiana",ZipCodeMin = 70001,ZipCodeMax = 71497},
            new UsState {StateCode = "ME",StateName = "Maine",ZipCodeMin = 3901,ZipCodeMax = 4992},
            new UsState {StateCode = "MD",StateName = "Maryland",ZipCodeMin = 20588,ZipCodeMax = 21930},
            new UsState {StateCode = "MA",StateName = "Massachusetts",ZipCodeMin = 1001,ZipCodeMax = 5544},
            new UsState {StateCode = "MI",StateName = "Michigan",ZipCodeMin = 48001,ZipCodeMax = 49971},
            new UsState {StateCode = "MN",StateName = "Minnesota",ZipCodeMin = 55001,ZipCodeMax = 56763},
            new UsState {StateCode = "MS",StateName = "Mississippi",ZipCodeMin = 38601,ZipCodeMax = 39776},
            new UsState {StateCode = "MO",StateName = "Missouri",ZipCodeMin = 63001,ZipCodeMax = 65899},
            new UsState {StateCode = "MT",StateName = "Montana",ZipCodeMin = 59001,ZipCodeMax = 59937},
            new UsState {StateCode = "NE",StateName = "Nebraska",ZipCodeMin = 68001,ZipCodeMax = 69367},
            new UsState {StateCode = "NV",StateName = "Nevada",ZipCodeMin = 88901,ZipCodeMax = 89883},
            new UsState {StateCode = "NH",StateName = "New Hampshire",ZipCodeMin = 3031,ZipCodeMax = 3897},
            new UsState {StateCode = "NJ",StateName = "New Jersey",ZipCodeMin = 7001,ZipCodeMax = 8989},
            new UsState {StateCode = "NM",StateName = "New Mexico",ZipCodeMin = 87001,ZipCodeMax = 88441},
            new UsState {StateCode = "NY",StateName = "New York",ZipCodeMin = 501,ZipCodeMax = 14975},
            new UsState {StateCode = "NC",StateName = "North Carolina",ZipCodeMin = 27006,ZipCodeMax = 28909},
            new UsState {StateCode = "ND",StateName = "North Dakota",ZipCodeMin = 58001,ZipCodeMax = 58856},
            new UsState {StateCode = "OH",StateName = "Ohio",ZipCodeMin = 43001,ZipCodeMax = 45999},
            new UsState {StateCode = "OK",StateName = "Oklahoma",ZipCodeMin = 73001,ZipCodeMax = 74966},
            new UsState {StateCode = "OR",StateName = "Oregon",ZipCodeMin = 97001,ZipCodeMax = 97920},
            new UsState {StateCode = "PA",StateName = "Pennsylvania",ZipCodeMin = 15001,ZipCodeMax = 19640},
            new UsState {StateCode = "RI",StateName = "Rhode Island",ZipCodeMin = 2801,ZipCodeMax = 2940},
            new UsState {StateCode = "SC",StateName = "South Carolina",ZipCodeMin = 29001,ZipCodeMax = 29948},
            new UsState {StateCode = "SD",StateName = "South Dakota",ZipCodeMin = 57001,ZipCodeMax = 57799},
            new UsState {StateCode = "TN",StateName = "Tennessee",ZipCodeMin = 37010,ZipCodeMax = 38589},
            new UsState {StateCode = "TX",StateName = "Texas",ZipCodeMin = 73301,ZipCodeMax = 88595},
            new UsState {StateCode = "UT",StateName = "Utah",ZipCodeMin = 84001,ZipCodeMax = 84791},
            new UsState {StateCode = "VT",StateName = "Vermont",ZipCodeMin = 5001,ZipCodeMax = 5907},
            new UsState {StateCode = "VA",StateName = "Virginia",ZipCodeMin = 20101,ZipCodeMax = 24658},
            new UsState {StateCode = "WA",StateName = "Washington",ZipCodeMin = 98001,ZipCodeMax = 99403},
            new UsState {StateCode = "WV",StateName = "West Virginia",ZipCodeMin = 24701,ZipCodeMax = 26886},
            new UsState {StateCode = "WI",StateName = "Wisconsin",ZipCodeMin = 53001,ZipCodeMax = 54990},
            new UsState {StateCode = "WY",StateName = "Wyoming",ZipCodeMin = 82001,ZipCodeMax = 83414}

        };
        return StateList;
    } // Get State List      

    public static string GetZipState(List<UsState> StateList, int ZipCode)
    {
        string? StateCode = string.Empty;
        // find State by Zip in Range
        if (StateList.Count > 0 && ZipCode > 0)
        {

            var lstItem = StateList.Find(x => ZipCode >= x.ZipCodeMin && ZipCode <= x.ZipCodeMax);
            if (lstItem != null)
            {
                StateCode = lstItem.StateCode;
            }
        }

        return StateCode;
    } // GetZipState



} // Common

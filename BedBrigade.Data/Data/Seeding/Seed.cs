using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Models;
using System.Data.SqlClient;
using BedBrigade.Common;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using static BedBrigade.Common.Common;
using System.Diagnostics;

namespace BedBrigade.Data.Seeding;

public class Seed
{

    // Table User
    private const string _seedUserPassword = "Password";


    private static readonly List<Role> _roles = new()
    {
        new Role {Name = RoleNames.NationalAdmin},
        new Role {Name = RoleNames.NationalEditor},
        new Role {Name = RoleNames.LocationAdmin},
        new Role {Name = RoleNames.LocationEditor},
        new Role {Name = RoleNames.LocationAuthor},
        new Role {Name = RoleNames.LocationScheduler},
        new Role {Name = RoleNames.LocationContributor},
        new Role {Name = RoleNames.LocationTreasurer},
        new Role {Name = RoleNames.LocationCommunications},
    };

    private static readonly List<Location> _locations = new()
    {
        new Location
        {
            Name = SeedConstants.SeedNationalName, Route = "/national", Address1 = "", Address2 = "", City = "Columbus",
            State = "Ohio", PostalCode = ""
        },
        new Location
        {
            Name = "Grove City Bed Brigade", Route = "/grove-city", Address1 = "4004 Thistlewood Dr", Address2 = "", City = "Grove City",
            State = "Ohio", PostalCode = "43123"
        },
        new Location
        {
            Name = "Rock City Polaris Bed Brigade", Route = "/rock-city-polaris", Address1 = "1250 Gemini Pl", Address2 = "", City = "Columbus",
            State = "Ohio", PostalCode = "43240"
        }
    };

    private static readonly List<User> _users = new()
    {
        //National Users
        new User { FirstName = SeedConstants.SeedNationalName, LastName = "Editor", Role = RoleNames.NationalEditor },
        new User { FirstName = SeedConstants.SeedNationalName, LastName = "Admin", Role = RoleNames.NationalAdmin },

        //Grove City Users
        new User { FirstName = SeedConstants.SeedGroveCityName, LastName = "Contributor", Role = RoleNames.LocationContributor },
        new User { FirstName = SeedConstants.SeedGroveCityName, LastName = "Author", Role = RoleNames.LocationAuthor },
        new User { FirstName = SeedConstants.SeedGroveCityName, LastName = "Editor", Role = RoleNames.LocationEditor },
        new User { FirstName = SeedConstants.SeedGroveCityName, LastName = "Scheduler", Role = RoleNames.LocationScheduler },
        new User { FirstName = SeedConstants.SeedGroveCityName, LastName = "Treasurer", Role = RoleNames.LocationTreasurer },
        new User
        {
            FirstName = SeedConstants.SeedGroveCityName, LastName = "Communications", Role = RoleNames.LocationCommunications
        },
        new User { FirstName = SeedConstants.SeedGroveCityName, LastName = "Admin", Role = RoleNames.LocationAdmin },

        //Rock City Users
        new User { FirstName = SeedConstants.SeedRockCityName, LastName = "Contributor", Role = RoleNames.LocationContributor },
        new User { FirstName = SeedConstants.SeedRockCityName, LastName = "Author", Role = RoleNames.LocationAuthor },
        new User { FirstName = SeedConstants.SeedRockCityName, LastName = "Editor", Role = RoleNames.LocationEditor },
        new User { FirstName = SeedConstants.SeedRockCityName, LastName = "Scheduler", Role = RoleNames.LocationScheduler },
        new User { FirstName = SeedConstants.SeedRockCityName, LastName = "Treasurer", Role = RoleNames.LocationTreasurer },
        new User
        {
            FirstName = SeedConstants.SeedRockCityName, LastName = "Communications", Role = RoleNames.LocationCommunications
        },
        new User { FirstName = SeedConstants.SeedRockCityName, LastName = "Admin", Role = RoleNames.LocationAdmin },
    };

    

    public static async Task SeedData(IDbContextFactory<DataContext> contextFactory)
    {

        await SeedConfigurations(contextFactory);
        await SeedLocations(contextFactory);
        await SeedContentsLogic.SeedContents(contextFactory);
        await SeedMedia(contextFactory);
        await SeedRoles(contextFactory);
        await SeedUser(contextFactory);
        await SeedVolunteersFor(contextFactory);
        await SeedVolunteers(contextFactory);
        await SeedDonations(contextFactory);
        await SeedBedRequests(contextFactory);
        SeedSchedules(contextFactory);

    }

    public static void SeedSchedules(IDbContextFactory<DataContext> contextFactory, bool bTruncateData = false)
    {
        Log.Logger.Information("Seed Schedules Started");
        string? sqlConnectionString = string.Empty; //"server=localhost\\sqlexpress;database=bedbrigade;trusted_connection=SSPI;Encrypt=False"; //connection string
        string script = String.Empty;

        using (var context = contextFactory.CreateDbContext())
        {
            sqlConnectionString = context.Database.GetConnectionString();
        if (bTruncateData) // clear table
        {
            script = "truncate table dbo.Schedules";
            Log.Logger.Information("Schedules will be truncated");
        }
        else // load data to table
        {
            var path = Environment.CurrentDirectory + "\\wwwroot\\data\\" + ($"CreateSchedules.sql");
            FileInfo file = new FileInfo(path);
            Log.Logger.Information("Schedules will be created");
            Log.Logger.Information("Schedules SQL script file: " + file.FullName);
            script = file.OpenText().ReadToEnd();
        }
            if (script.Length > 0)
            {
                SqlConnection tmpConn;
                tmpConn = new SqlConnection();
                tmpConn.ConnectionString = sqlConnectionString;

                SqlCommand myCommand = new SqlCommand(script, tmpConn);
                try
                {
                    tmpConn.Open();
                    var result = myCommand.ExecuteNonQuery();
                    Log.Logger.Information("Schedules records affected: "+result.ToString());

                }
                catch (Exception ex)
                {
                    Log.Logger.Information(ex.Message);
                }
            } //context

        }
    } // Seed Schedules



    private static async Task SeedConfigurations(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedConfigurations Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.Configurations.AnyAsync()) return;

            var configurations = new List<Configuration>
            {
                new()
                {
                    ConfigurationKey = ConfigNames.FromEmailAddress,
                    ConfigurationValue = "webmaster@bedbrigade.org",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.HostName,
                    ConfigurationValue = "mail.bedbrigade.org",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.Port,
                    ConfigurationValue = "8889",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.TokenExpiration,
                    ConfigurationValue = "24",
                    Section = ConfigSection.System
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.AllowedFileExtensions,
                    ConfigurationValue = ".jpg, .png, .pdf, .webp",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.AllowedVideoExtensions,
                    ConfigurationValue = ".mp4",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MediaFolder,
                    ConfigurationValue = "media",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MainMediaSubFolder,
                    ConfigurationValue = "national",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MaxFileSize,
                    ConfigurationValue = "104857600",
                    Section = ConfigSection.Media    
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MaxVideoSize,
                    ConfigurationValue = "262144000",
                    Section = ConfigSection.Media
                },
                new()
                {
                    ConfigurationKey = ConfigNames.IsCachingEnabled,
                    ConfigurationValue = "true",
                    Section = ConfigSection.System
                },
                new()
                {
                    ConfigurationKey = ConfigNames.BedBrigadeNearMeMaxMiles,
                    ConfigurationValue = "30",
                    Section = ConfigSection.System
                },
            };

            await context.Configurations.AddRangeAsync(configurations);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Creates the Location table in the DB and also creates location folders in the wwwroot/media folder based upon the route data
    /// </summary>
    /// <param name="contextFactory"></param>
    /// <returns></returns>
    private static async Task SeedLocations(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedLocations Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.Locations.AnyAsync()) return;

            try
            {
                foreach (var location in _locations)
                {
                    var loc = location.Route + "/pages";
                    loc.CreateDirectory();
                    context.Locations.Add(location);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Information($"Location seed error: {ex.Message}");
                throw;
            }
        }
    }

    private static async Task SeedMedia(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedMedia Started");

        using (var context = contextFactory.CreateDbContext())
        {
            try
            {
                if (!await context.Media.AnyAsync(m => m.FileName == "Logo")) // table Media does not have site logo
                {
                    // var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                    // add the first reciord in Media table with National Logo
                    context.Media.Add(new Media
                    {
                        LocationId = (int)LocationNumber.National,
                        FileName = "logo",
                        MediaType = "png",
                        FilePath = "media/national",
                        FileSize = 9827,
                        AltText = "Bed Brigade National Logo",
                        FileStatus = "seed",
                        FileUse = FileUse.Unknown,
                        CreateDate = DateTime.Now,
                        UpdateDate = DateTime.Now,
                        CreateUser = SeedConstants.SeedUserName,
                        UpdateUser = SeedConstants.SeedUserName,
                        MachineName = Environment.MachineName
                    });

                    await context.SaveChangesAsync();
                } // add the first media row             
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seed media: {ex.Message}");
                throw;
            }
        }
    } // Seed Media

    private static async Task SeedRoles(IDbContextFactory<DataContext> _contextFactory)
    {
        Log.Logger.Information("SeedRoles Started");

        using (var context = _contextFactory.CreateDbContext())
        {
            if (await context.Roles.AnyAsync()) return;
            try
            {
                await context.Roles.AddRangeAsync(_roles);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroor adding roles {ex.Message}");
                throw;
            }
        }
    }
    private static async Task SeedUser(IDbContextFactory<DataContext> _contextFactory)
    {
        Log.Logger.Information("SeedUser Started");

        using (var context = _contextFactory.CreateDbContext())
        {
            foreach (var user in _users)
            {
                if (!await context.Users.AnyAsync(u => u.UserName == $"{user.FirstName}{user.LastName}"))
                {
                    try
                    {
                        SeedRoutines.CreatePasswordHash(_seedUserPassword, out byte[] passwordHash, out byte[] passwordSalt);

                        int locationId;

                        switch (user.FirstName)
                        {
                            case SeedConstants.SeedNationalName:
                                locationId = (int)LocationNumber.National;
                                break;
                            case SeedConstants.SeedGroveCityName:
                                locationId = (int)LocationNumber.GroveCity;
                                break;
                            case SeedConstants.SeedRockCityName:
                                locationId = (int)LocationNumber.RockCity;
                                break;
                            default:
                                throw new Exception("Invalid location name: " + user.FirstName);
                        }

                        // Create the user
                        var newUser = new User
                        {
                            UserName = $"{user.FirstName}{user.LastName}",
                            LocationId = locationId,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = $"{user.FirstName}.{user.LastName}@bedBrigade.org".ToLower(),
                            Phone = GeneratePhoneNumber(),
                            Role = user.Role,
                            FkRole = context.Roles.FirstOrDefault(r => r.Name == user.Role).RoleId,
                            PasswordHash = passwordHash,
                            PasswordSalt = passwordSalt,
                            PersistBedRequest = String.Empty,
                            PersistConfig = String.Empty,
                            PersistDonation = string.Empty,
                            PersistLocation = string.Empty,
                            PersistMedia = string.Empty,
                            PersistPages = string.Empty,
                            PersistUser = string.Empty,
                            PersistVolunteers = string.Empty
                        };
                        await context.Users.AddAsync(newUser);
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SaveChanges Error {ex.Message}");
                        throw;
                    }
                }
            }
        }
    }

    private static async Task SeedVolunteersFor(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedVolunteersFor Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.VolunteersFor.AnyAsync()) return;
            List<VolunteerFor> VolunteeringFor = new List<VolunteerFor>
            {
                new VolunteerFor{Name = "Bed Building" },
                new VolunteerFor{Name = "Bed Delivery" },
                new VolunteerFor { Name = "Event Planning" },
                new VolunteerFor { Name = "New Option" },
                new VolunteerFor { Name = "Other" }
            };
            try
            {
                await context.VolunteersFor.AddRangeAsync(VolunteeringFor);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding volunteers for {ex.Message}");
                throw;
            }
        }

    }
    private static async Task SeedVolunteers(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedVolunteers Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.Volunteers.AnyAsync()) return;

            List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
            List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
            List<bool> YesOrNo = new List<bool> { true, false };
            List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
            List<VolunteerFor> volunteersFor = context.VolunteersFor.ToList();
            List<Location> locations = context.Locations.ToList();
            var item = locations.Single(r => r.LocationId == (int) LocationNumber.National);
            if (item != null)
            {
                locations.Remove(item);
            }

            for (var i = 0; i <= 100; i++)
            {
                var firstName = FirstNames[new Random().Next(FirstNames.Count - 1)];
                var lastName = LastNames[new Random().Next(LastNames.Count - 1)];
                int firstThree, nextThree, lastFour;
                var phoneNumber = GeneratePhoneNumber();
                var location = locations[new Random().Next(locations.Count - 1)];
                var volunteeringFor = volunteersFor[new Random().Next(volunteersFor.Count - 1)];
                Volunteer volunteer = new()
                {
                    LocationId = location.LocationId,
                    VolunteeringForId = volunteeringFor.VolunteerForId,
                    VolunteeringForDate = DateTime.Now.AddDays(new Random().Next(60)),
                    IHaveVolunteeredBefore = YesOrNo[new Random().Next(YesOrNo.Count - 1)],
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                    Phone = phoneNumber,
                    IHaveAMinivan = YesOrNo[new Random().Next(YesOrNo.Count)],
                    IHaveAnSUV = YesOrNo[new Random().Next(YesOrNo.Count)],
                    IHaveAPickupTruck = YesOrNo[new Random().Next(YesOrNo.Count)]
                };
                try
                {
                    await context.AddAsync(volunteer);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error in Volunteer: {ex.Message}");
                    throw;
                }
            }
        }
    }

    private static async Task SeedDonations(IDbContextFactory<DataContext> contextFactory)
    {
        try
        {
            Log.Logger.Information("SeedDonations Started");

            using (var context = contextFactory.CreateDbContext())
            {
                if (await context.Donations.AnyAsync()) return;

                List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
                List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
                List<bool> YesOrNo = new List<bool> { true, false };
                List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
                List<Location> locations = await context.Locations.ToListAsync();
                var item = locations.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    locations.Remove(item);
                }

                for (var i = 0; i < 100; i++)
                {
                    var location = locations[new Random().Next(locations.Count - 1)];
                    var firstName = FirstNames[new Random().Next(FirstNames.Count - 1)];
                    var lastName = LastNames[new Random().Next(LastNames.Count - 1)];
                    Donation donation = new()
                    {
                        LocationId = location.LocationId,
                        Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                        Amount = new decimal(new Random().NextDouble() * 1000),
                        TransactionId = new Random().Next(233999, 293737).ToString(),
                        FirstName = firstName,
                        LastName = lastName,
                        TaxFormSent = YesOrNo[new Random().Next(YesOrNo.Count - 1)]
                    };

                    await context.Donations.AddAsync(donation);
                }
                await context.SaveChangesAsync();
            }
        }
        catch (DbException ex)
        {
            Log.Logger.Error("Db Error {0} {1}", ex.ToString(), ex.StackTrace);
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in donations {0} {1}", ex.ToString(), ex.StackTrace);
        }
    }

    private static async Task SeedBedRequests(IDbContextFactory<DataContext> contextFactory)
    {
        try
        {
            Log.Logger.Information("Seed BedRequest Started");

            using (var context = contextFactory.CreateDbContext())
            {
                if (await context.BedRequests.AnyAsync()) return;

                List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
                List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
                List<bool> YesOrNo = new List<bool> { true, false };
                List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
                List<string> City = new List<string> { "Columbus", "Cleveland", "Cincinnati", "Canton", "Youngston", "Springfield", "Middletown", "Beavercreek" };
                List<string> StreetName = new List<string> { "E. Bella Ln", "W. Chandler Blvd", "25 St.", "4th Ave.", "G Ave.", "Indian Wells CT.", "N. Arizona" };
                List<Location> locations = await context.Locations.ToListAsync();
                var item = locations.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    locations.Remove(item);

                }
                for (var i = 0; i < 30; i++)
                {
                    var location = locations[new Random().Next(locations.Count - 1)];
                    var firstName = FirstNames[new Random().Next(FirstNames.Count - 1)];
                    var lastName = LastNames[new Random().Next(LastNames.Count - 1)];
                    BedRequest bedRequest = new()
                    {
                        LocationId = location.LocationId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                        Phone = GeneratePhoneNumber(),
                        Street = $"{new Random().Next(99999).ToString()} {StreetName[new Random().Next(StreetName.Count -1)]}",
                        City = City[new Random().Next(City.Count - 1)],
                        State = "Ohio",
                        PostalCode = new Random().Next(43001, 43086).ToString(),
                        NumberOfBeds = new Random().Next(1, 4),
                        Status = (BedRequestStatus) new Random().Next(1,4),
                        TeamNumber = new Random().Next(1, 5),
                        DeliveryDate = DateTime.Now.AddDays(new Random().Next(10)),
                        Notes = string.Empty
                    };

                    await context.BedRequests.AddAsync(bedRequest);
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (DbException ex)
        {
            Log.Logger.Error("Db Error {0} {1}", ex.ToString(), ex.StackTrace);
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Error in bedrequest {0} {1}", ex.ToString(), ex.StackTrace);
        }
    }


    private static string GeneratePhoneNumber()
    {
        var firstThree = new Random().Next(291, 861);
        var nextThree = new Random().Next(200, 890);
        var lastFour = new Random().Next(1000, 9999);
        return $"({firstThree}) {nextThree}-{lastFour}";
    }
}






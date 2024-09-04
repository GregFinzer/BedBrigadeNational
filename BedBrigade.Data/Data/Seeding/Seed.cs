using BedBrigade.Data.Data.Seeding;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;

using System.Diagnostics;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using Bogus;

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
        await SeedMetroAreas(contextFactory);
        await SeedContentsLogic.SeedContents(contextFactory);
        await SeedMedia(contextFactory);
        await SeedRoles(contextFactory);
        await SeedUser(contextFactory);
        await SeedVolunteersFor(contextFactory);
        await SeedVolunteers(contextFactory);
        await SeedDonations(contextFactory);
        await SeedBedRequests(contextFactory);
        await SeedSchedules(contextFactory);
    }

    private static async Task SeedMetroAreas(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedMetroAreas Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.MetroAreas.AnyAsync()) return;

            try
            {
                MetroArea metroArea = new MetroArea
                {
                    Name = "None"
                };

                SeedRoutines.SetMaintFields(metroArea);
                await context.MetroAreas.AddAsync(metroArea);

                metroArea = new MetroArea
                {
                    Name = "Columbus, Ohio"
                };

                SeedRoutines.SetMaintFields(metroArea);
                await context.MetroAreas.AddAsync(metroArea);

                await context.SaveChangesAsync();

                var groveCity = await context.Locations.FirstOrDefaultAsync(o => o.LocationId == (int)LocationNumber.GroveCity);
                if (groveCity != null)
                {
                    groveCity.MetroAreaId = metroArea.MetroAreaId;
                    context.Locations.Update(groveCity);
                    await context.SaveChangesAsync();
                }

                var rockCity = await context.Locations.FirstOrDefaultAsync(o => o.LocationId == (int)LocationNumber.RockCity);
                if (rockCity != null)
                {
                    rockCity.MetroAreaId = metroArea.MetroAreaId;
                    context.Locations.Update(rockCity);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Information($"MetroArea seed error: {ex.Message}");
                throw;
            }
        }
    }

    public static async Task SeedSchedules(IDbContextFactory<DataContext> contextFactory)
    {
        using (DataContext context = contextFactory.CreateDbContext())
        {
            if (await context.Schedules.AnyAsync()) return;

            await SeedBuildSchedule(context);
            await SeedDeliverySchedule(context);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedBuildSchedule(DataContext context)
    {
        Log.Logger.Information("SeedBuildSchedule Started");

        DateTime currentDate = DateTime.Today;

        for (int i = 0; i < 12; i++)
        {
            // Get the first day of the current month
            DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);

            // Calculate the first Saturday of the month
            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)firstDayOfMonth.DayOfWeek + 7) % 7;

            DateTime firstSaturday = firstDayOfMonth.AddDays(daysUntilSaturday);

            Schedule schedule = new Schedule
            {
                LocationId = (int)LocationNumber.GroveCity,
                EventName = "Build",
                EventNote =
                    "Come build beds with us at our shop at 4004 Thistlewood Drive, Grove City. Look for signs.",
                EventStatus = EventStatus.Scheduled,
                EventType = EventType.Build,
                EventDateScheduled = firstSaturday.AddHours(9),
                EventDurationHours = 3,
                VolunteersMax = 20,
                VolunteersRegistered = 0,
                DeliveryVehiclesRegistered = 0
            };

            SeedRoutines.SetMaintFields(schedule);
            await context.Schedules.AddAsync(schedule);

            // Move to the next month
            currentDate = currentDate.AddMonths(1);
        }
    }


    public static async Task SeedDeliverySchedule(DataContext context)
    {
        Log.Logger.Information("SeedDeliverySchedule Started");

        // Calculate the first Saturday
        int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0)
        {
            daysUntilSaturday = 7; // If today is Saturday, schedule for the next Saturday
        }

        DateTime nextSaturday = DateTime.Today.AddDays(daysUntilSaturday);

        for (int i = 0; i < 52; i++)
        {
            Schedule schedule = new Schedule
            {
                LocationId = (int)LocationNumber.GroveCity,
                EventName = "Delivery",
                EventNote =
                    "Come deliver beds with us at our shop at 4004 Thistlewood Drive, Grove City. Look for signs.",
                EventStatus = EventStatus.Scheduled,
                EventType = EventType.Delivery,
                EventDateScheduled = nextSaturday.AddDays(i * 7).AddHours(9),
                EventDurationHours = 3,
                VolunteersMax = 20,
                VolunteersRegistered = 0,
                DeliveryVehiclesRegistered = 0
            };

            SeedRoutines.SetMaintFields(schedule);
            await context.Schedules.AddAsync(schedule);
        }
    }



    private static async Task SeedConfigurations(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedConfigurations Started");

        using (var context = contextFactory.CreateDbContext())
        {
            Log.Logger.Information("Created DBContext");
            if (await context.Configurations.AnyAsync()) return;

            Log.Logger.Information("No configurations found, adding");
            var configurations = new List<Configuration>
            {
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
                new()
                {
                    ConfigurationKey = ConfigNames.ReCaptchaSiteKey,
                    ConfigurationValue = "6LeDtS0qAAAAANLi2IY68WW555JXAWIelpoZQIWO",
                    Section = ConfigSection.System
                },
                new()
                {
                    ConfigurationKey = ConfigNames.ReCaptchaSecret,
                    ConfigurationValue = "6LeDtS0qAAAAANGrgPxMV2vTcgVG1e01KaRGjuqL",
                    Section = ConfigSection.System
                },
                new()
                {
                    ConfigurationKey = ConfigNames.FromEmailAddress,
                    ConfigurationValue = "devtest@bedbrigadecolumbus.org",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailBeginHour,
                    ConfigurationValue = "0",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailEndHour,
                    ConfigurationValue = "23",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailBeginDayOfWeek,
                    ConfigurationValue = "0",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailEndDayOfWeek,
                    ConfigurationValue = "6",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxSendPerMinute,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxSendPerHour,
                    ConfigurationValue = "60",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxSendPerDay,
                    ConfigurationValue = "1440",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailLockWaitMinutes,
                    ConfigurationValue = "10",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailKeepDays,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxPerChunk,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailUseFileMock,
                    ConfigurationValue = "true",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.FromEmailDisplayName,
                    ConfigurationValue = "Bed Brigade NoReply",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailHost,
                    ConfigurationValue = "mail5019.site4now.net",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailPort,
                    ConfigurationValue = "8889",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailUserName,
                    ConfigurationValue = "devtest@bedbrigadecolumbus.org",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmailPassword,
                    ConfigurationValue = "AskGregForPassword",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = ConfigNames.DisplayIdFields,
                    ConfigurationValue = "No",
                    Section = ConfigSection.System
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EmptyGridText,
                    ConfigurationValue = "No matching records found",
                    Section = ConfigSection.System
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EventCutOffTimeDays,
                    ConfigurationValue = "4",
                    Section = ConfigSection.System
                },
            };

            SeedRoutines.SetMaintFields(configurations);
            await context.Configurations.AddRangeAsync(configurations);
            Log.Logger.Information("After AddRangeAsync");
            await context.SaveChangesAsync();
            Log.Logger.Information("After SaveChanges");
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
                SeedRoutines.SetMaintFields(_locations);

                foreach (var location in _locations)
                {
                    var loc = location.Route + "/pages";
                    FileUtil.CreateMediaSubDirectory(loc);
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
                        CreateDate = DateTime.UtcNow,
                        UpdateDate = DateTime.UtcNow,
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
        SeedRoutines.SetMaintFields(_users);

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
                            PasswordSalt = passwordSalt
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
            List<VolunteerFor> volunteeringForList = new List<VolunteerFor>
            {
                new VolunteerFor{Name = "Bed Building" },
                new VolunteerFor{Name = "Bed Delivery" },
                new VolunteerFor { Name = "Event Planning" },
                new VolunteerFor { Name = "New Option" },
                new VolunteerFor { Name = "Other" }
            };

            SeedRoutines.SetMaintFields(volunteeringForList);

            try
            {
                await context.VolunteersFor.AddRangeAsync(volunteeringForList);
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

            List<bool> YesOrNo = new List<bool> { true, false };
            List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
            List<VolunteerFor> volunteersFor = context.VolunteersFor.ToList();
            List<Location> locations = context.Locations.ToList();
            var item = locations.Single(r => r.LocationId == (int)LocationNumber.National);
            if (item != null)
            {
                locations.Remove(item);
            }

            for (var i = 0; i <= 100; i++)
            {
                var faker = new Faker();
                var firstName = faker.Name.FirstName();
                var lastName = faker.Name.LastName();
                var phoneNumber = GeneratePhoneNumber();
                var location = locations[new Random().Next(locations.Count - 1)];
                Volunteer volunteer = new()
                {
                    LocationId = location.LocationId,
                    IHaveVolunteeredBefore = YesOrNo[new Random().Next(YesOrNo.Count - 1)],
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                    Phone = phoneNumber,
                    VehicleType = (VehicleType)new Random().Next(0, 3),
                };
                try
                {
                    SeedRoutines.SetMaintFields(volunteer);
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
                    var faker = new Faker();
                    var firstName = faker.Name.FirstName();
                    var lastName = faker.Name.LastName();
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

                    SeedRoutines.SetMaintFields(donation);
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
                    var faker = new Faker();
                    var firstName = faker.Name.FirstName();
                    var lastName = faker.Name.LastName();
                    BedRequest bedRequest = new()
                    {
                        LocationId = location.LocationId,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                        Phone = GeneratePhoneNumber(),
                        Street = $"{new Random().Next(99999).ToString()} {StreetName[new Random().Next(StreetName.Count - 1)]}",
                        City = City[new Random().Next(City.Count - 1)],
                        State = "Ohio",
                        PostalCode = new Random().Next(43001, 43086).ToString(),
                        NumberOfBeds = new Random().Next(1, 4),
                        Status = (BedRequestStatus)new Random().Next(1, 4),
                        TeamNumber = new Random().Next(1, 5),
                        DeliveryDate = DateTime.UtcNow.AddDays(new Random().Next(10)),
                        Notes = string.Empty
                    };

                    SeedRoutines.SetMaintFields(bedRequest);
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






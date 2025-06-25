using BedBrigade.Data.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using Bogus;
using Microsoft.EntityFrameworkCore.Internal;

namespace BedBrigade.Data.Seeding;

public static class Seed
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
            Name = SeedConstants.SeedNationalName, 
            Route = "/national", 
            MailingCity = "Columbus", 
            MailingState = "Ohio", 
            IsActive = true,
            TimeZoneId = Defaults.DefaultTimeZoneId
        },
        new Location
        {
            Name = "Grove City Bed Brigade", 
            Route = "/grove-city", 
            MailingAddress = "1788 Killdeer Rd", 
            MailingCity = "Grove City",
            MailingState = "OH", 
            MailingPostalCode = "43123",
            BuildAddress = "4004 Thistlewood Dr",
            BuildCity = "Grove City",
            BuildState= "OH",
            BuildPostalCode= "43123",
            IsActive = true,
            Latitude = 39.882527M,
            Longitude = -83.042266M,
            TimeZoneId = Defaults.DefaultTimeZoneId

        },
        new Location
        {
            Name = "Rock City Polaris Bed Brigade", Route = "/rock-city-polaris", 
            MailingAddress = "171 E. Fifth Ave", 
            MailingCity = "Columbus",
            MailingState = "OH", 
            MailingPostalCode = "43201",
            BuildAddress = "171 E. Fifth Ave",
            BuildCity = "Columbus",
            BuildState = "OH",
            BuildPostalCode = "43201",
            IsActive = true,
            Latitude = 39.986799M,
            Longitude = -83.0006655M,
            TimeZoneId = Defaults.DefaultTimeZoneId
        }

    };

    private static List<Configuration> _configurations = SeedConfigLogic.AllConfigurationForSeeding();

    private static readonly List<User> _developmentUsers = new()
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
        new User { FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Contributor", Role = RoleNames.LocationContributor },
        new User { FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Author", Role = RoleNames.LocationAuthor },
        new User { FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Editor", Role = RoleNames.LocationEditor },
        new User { FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Scheduler", Role = RoleNames.LocationScheduler },
        new User { FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Treasurer", Role = RoleNames.LocationTreasurer },
        new User
        {
            FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Communications", Role = RoleNames.LocationCommunications
        },
        new User { FirstName = SeedConstants.SeedRockCityPolarisName, LastName = "Admin", Role = RoleNames.LocationAdmin },
    };

    private static readonly List<User> _productionUsers = new()
    {
        new User { FirstName = "Greg", 
            LastName = "Finzer", 
            Role = RoleNames.NationalAdmin, 
            Email="gfinzer@hotmail.com", 
            LocationId = Defaults.GroveCityLocationId,
            Phone = string.Empty
        }
    };


    public static async Task SeedData(IDbContextFactory<DataContext> contextFactory)
    {
        await SeedConfigurations(contextFactory);
        await SeedLocations(contextFactory);
        await UpdateLocations(contextFactory);
        await SeedMetroAreas(contextFactory);
        await SeedContentsLogic.SeedContents(contextFactory);
        await SeedMedia(contextFactory);
        await SeedRoles(contextFactory);
        await SeedUsers(contextFactory);
        await SeedVolunteersFor(contextFactory);
        await SeedVolunteers(contextFactory);
        await SeedDonationCampaign(contextFactory);
        await SeedDonations(contextFactory);
        await SeedBedRequests(contextFactory);
        await SeedSchedules(contextFactory);
        await SeedStoriesLogic.SeedStories(contextFactory);
        await SeedNewsLogic.SeedNews(contextFactory);
        await SeedTranslationsLogic.SeedTranslationsAsync(contextFactory);
        await SeedTranslationsLogic.SeedContentTranslations(contextFactory);
        await SeedSpokenLanguages(contextFactory);
        await SeedNewsletters(contextFactory);
    }

    private static async Task SeedNewsletters(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedNewsletters Started");

        using (var context = contextFactory.CreateDbContext())
        {
            var existingNewsletters = await context.Newsletters.ToListAsync();

            if (existingNewsletters.Any())
                return;

            try
            {
                Newsletter newsletter = new Newsletter
                {
                    Name = "Rock City Polaris Newsletter",
                    LocationId = Defaults.RockCityPolarisLocationId
                };

                SeedRoutines.SetMaintFields(newsletter);
                await context.Newsletters.AddAsync(newsletter);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Information($"Newsletters seed error: {ex.Message}");
                throw;
            }
        }
    }

    private static async Task UpdateLocations(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("UpdateLocations Started");

        using (var context = contextFactory.CreateDbContext())
        {
            var existingLocations =
                await context.Locations.Where(o => o.LocationId > Defaults.NationalLocationId && o.Longitude == null).ToListAsync();

            if (!existingLocations.Any())
                return;

            try
            {
                foreach (var existingLocation in existingLocations)
                {
                    var newData = _locations.First(o => o.Name == existingLocation.Name);
                    existingLocation.Latitude = newData.Latitude;
                    existingLocation.Longitude = newData.Longitude;
                    existingLocation.MailingAddress = newData.MailingAddress;
                    existingLocation.MailingCity = newData.MailingCity;
                    existingLocation.MailingState = newData.MailingState;
                    existingLocation.MailingPostalCode = newData.MailingPostalCode;

                    existingLocation.BuildAddress = newData.BuildAddress;
                    existingLocation.BuildCity = newData.BuildCity;
                    existingLocation.BuildState = newData.BuildState;
                    existingLocation.BuildPostalCode = newData.BuildPostalCode;

                    SeedRoutines.SetMaintFields(existingLocation);
                    context.Locations.Update(existingLocation);
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Information($"Location seed error: {ex.Message}");
                throw;
            }
        }
    }
    private static async Task SeedSpokenLanguages(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedSpokenLanguages Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.SpokenLanguages.AnyAsync()) return;

            try
            {
                List<SpokenLanguage> spokenLanguages = new List<SpokenLanguage>()
                {
                    new SpokenLanguage() { Name = "Spanish", Value = "Spanish" },
                    new SpokenLanguage() { Name = "Haitian Creole", Value = "Haitian Creole" },
                    new SpokenLanguage() { Name = "French", Value = "French" }
                };

                SeedRoutines.SetMaintFields(spokenLanguages);
                await context.SpokenLanguages.AddRangeAsync(spokenLanguages);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Information($"SeedSpokenLanguages seed error: {ex.Message}");
                throw;
            }
        }
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
                    groveCity.IsActive = true;
                    context.Locations.Update(groveCity);
                    await context.SaveChangesAsync();
                }

                var rockCityPolaris = await context.Locations.FirstOrDefaultAsync(o => o.LocationId == (int)LocationNumber.RockCityPolaris);
                if (rockCityPolaris != null)
                {
                    rockCityPolaris.MetroAreaId = metroArea.MetroAreaId;
                    rockCityPolaris.IsActive = true;
                    context.Locations.Update(rockCityPolaris);
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
        if (WebHelper.IsProduction())
        {
            Log.Logger.Information("Seed Schedules Skipped since we are in Production");
            return;
        }

        using (DataContext context = contextFactory.CreateDbContext())
        {
            if (await context.Schedules.AnyAsync()) return;

            await SeedGroveCityBuildSchedule(context);
            await SeedGroveCityDeliverySchedule(context);
            await SeedRockCityPolarisBuildSchedule(context);
            await SeedRockCityPolarisDeliverySchedule(context);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedGroveCityBuildSchedule(DataContext context)
    {
        Log.Logger.Information("SeedGroveCityBuildSchedule Started");

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

    public static async Task SeedRockCityPolarisBuildSchedule(DataContext context)
    {
        Log.Logger.Information("SeedRockCityPolarisBuildSchedule Started");

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
                LocationId = (int)LocationNumber.RockCityPolaris,
                EventName = "Build",
                EventNote =
                    "Come build beds with us at our shop at 171 E. 5th Ave, Columbus, OH.",
                EventStatus = EventStatus.Scheduled,
                EventType = EventType.Build,
                EventDateScheduled = firstSaturday.AddHours(9),
                EventDurationHours = 3,
                VolunteersMax = 20,
                VolunteersRegistered = 0,
                DeliveryVehiclesRegistered = 0,
                Address = "171 E. 5th Ave",
                City = "Columbus",
                State = "OH"
            };

            SeedRoutines.SetMaintFields(schedule);
            await context.Schedules.AddAsync(schedule);

            // Move to the next month
            currentDate = currentDate.AddMonths(1);
        }
    }

    public static async Task SeedRockCityPolarisDeliverySchedule(DataContext context)
    {
        Log.Logger.Information("SeedRockCityPolarisDeliverySchedule Started");

        DateTime currentDate = DateTime.Today;

        for (int i = 0; i < 12; i++)
        {
            // Get the first day of the current month
            DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);

            // Calculate the first Saturday of the month
            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)firstDayOfMonth.DayOfWeek + 7) % 7;

            DateTime firstSaturday = firstDayOfMonth.AddDays(daysUntilSaturday);
            DateTime secondSaturday = firstSaturday.AddDays(7);

            Schedule schedule = new Schedule
            {
                LocationId = (int)LocationNumber.RockCityPolaris,
                EventName = "Delivery",
                EventNote =
                    "Come deliver beds with us from our shop at 171 E. 5th Ave, Columbus, OH.",
                EventStatus = EventStatus.Scheduled,
                EventType = EventType.Delivery,
                EventDateScheduled = secondSaturday.AddHours(9),
                EventDurationHours = 3,
                VolunteersMax = 20,
                VolunteersRegistered = 0,
                DeliveryVehiclesRegistered = 0,
                Address = "171 E. 5th Ave",
                City = "Columbus",
                State = "OH"
            };

            SeedRoutines.SetMaintFields(schedule);
            await context.Schedules.AddAsync(schedule);

            // Move to the next month
            currentDate = currentDate.AddMonths(1);
        }
    }

    public static async Task SeedGroveCityDeliverySchedule(DataContext context)
    {
        Log.Logger.Information("SeedGroveCityDeliverySchedule Started");

        // Calculate the first Saturday
        int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)DateTime.Today.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0)
        {
            daysUntilSaturday = 7; // If today is Saturday, schedule for the next Saturday
        }

        DateTime nextSaturday = DateTime.Today.AddDays(daysUntilSaturday);

        // Skip the first Saturday of the month if it's within the first 7 days of the month
        if (nextSaturday.Day <= 7)
        {
            nextSaturday = nextSaturday.AddDays(7);  // Move to the second Saturday
        }

        // Loop through the next 52 weeks
        for (int i = 0; i < 52; i++)
        {
            // Calculate the current Saturday
            DateTime currentSaturday = nextSaturday.AddDays(i * 7);

            // Check if the current Saturday is the first Saturday of the month
            if (currentSaturday.Day <= 7)
            {
                // Skip the first Saturday by moving to the second Saturday
                currentSaturday = currentSaturday.AddDays(7);
            }

            // Create the schedule
            Schedule schedule = new Schedule
            {
                LocationId = (int)LocationNumber.GroveCity,
                EventName = "Delivery",
                EventNote =
                    "Come deliver beds with us at our shop at 4004 Thistlewood Drive, Grove City. Look for signs.",
                EventStatus = EventStatus.Scheduled,
                EventType = EventType.Delivery,
                EventDateScheduled = currentSaturday.AddHours(9), // Set the scheduled time
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
            // Read all existing configurations into a list
            var existingConfigurations = await context.Configurations.ToListAsync();

            if (existingConfigurations.Any())
            {
                Log.Logger.Information("Existing configurations found, checking for new ones to add");
            }
            else
            {
                Log.Logger.Information("No configurations found, adding all");
            }

            var configurationsToAdd = new List<Configuration>();

            foreach (var newConfig in _configurations)
            {
                if (!existingConfigurations.Any(c => c.ConfigurationKey == newConfig.ConfigurationKey && c.LocationId == newConfig.LocationId))
                {
                    configurationsToAdd.Add(newConfig);
                }
            }

            if (configurationsToAdd.Any())
            {
                Log.Logger.Information($"Adding {configurationsToAdd.Count} new configurations");
                SeedRoutines.SetMaintFields(configurationsToAdd);
                await context.Configurations.AddRangeAsync(configurationsToAdd);
                await context.SaveChangesAsync();
                Log.Logger.Information("New configurations added successfully");
            }
            else
            {
                Log.Logger.Information("No new configurations to add");
            }
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
                    // add the first record in Media table with National Logo
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
    private static async Task SeedUsers(IDbContextFactory<DataContext> _contextFactory)
    {
        Log.Logger.Information("SeedUsers Started");
        List<User> users;

        if (WebHelper.IsProduction())
        {
            users = _productionUsers;
        }
        else
        {
            users = _developmentUsers;
        }

        using (var context = _contextFactory.CreateDbContext())
        {
            foreach (var user in users)
            {
                if (!await context.Users.AnyAsync(u => u.UserName == $"{user.FirstName}{user.LastName}"))
                {
                    try
                    {
                        SeedRoutines.CreatePasswordHash(_seedUserPassword, out byte[] passwordHash, out byte[] passwordSalt);

                        if (WebHelper.IsDevelopment())
                        {
                            SetDevelopmentUserFields(user);
                        }

                        // Create the user
                        var newUser = new User
                        {
                            UserName = $"{user.FirstName}{user.LastName}",
                            LocationId = user.LocationId,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email,
                            Phone = user.Phone,
                            Role = user.Role,
                            FkRole = context.Roles.FirstOrDefault(r => r.Name == user.Role).RoleId,
                            PasswordHash = passwordHash,
                            PasswordSalt = passwordSalt
                        };

                        SeedRoutines.SetMaintFields(newUser);
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

    private static void SetDevelopmentUserFields(User user)
    {
        // Set the location based on the user's first name (location name
        switch (user.FirstName)
        {
            case SeedConstants.SeedNationalName:
                if (user.LastName == "Admin")
                {
                    user.LocationId = Defaults.GroveCityLocationId;
                }
                else
                {
                    user.LocationId = Defaults.NationalLocationId;
                }
                break;
            case SeedConstants.SeedGroveCityName:
                user.LocationId = (int)LocationNumber.GroveCity;
                break;
            case SeedConstants.SeedRockCityPolarisName:
                user.LocationId = (int)LocationNumber.RockCityPolaris;
                break;
            default:
                throw new Exception("Invalid location name: " + user.FirstName);
        }

        user.Email = $"{user.FirstName}.{user.LastName}@bedBrigade.org".ToLower();
        user.Phone = GeneratePhoneNumber();
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
        if (WebHelper.IsProduction())
        {
            Log.Logger.Information("Seed Volunteers Skipped since we are in Production");
            return;
        }

        Log.Logger.Information("SeedVolunteers Started");

        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.Volunteers.AnyAsync()) return;

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
                    IHaveVolunteeredBefore = new Random().Next(2) == 0,
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

    public static async Task SeedDonationCampaign(IDbContextFactory<DataContext> contextFactory)
    {
        Log.Logger.Information("SeedDonationCampaign Started");
        using (var context = contextFactory.CreateDbContext())
        {
            if (await context.DonationCampaigns.AnyAsync()) return;
            try
            {
                var locations = await context.Locations.ToListAsync();

                foreach (var location in locations) 
                {
                    DonationCampaign donationCampaign = new()
                    {
                        CampaignName = Defaults.DefaultDonationCampaignName,
                        LocationId = location.LocationId,
                        StartDate = DateTime.UtcNow
                    };
                    SeedRoutines.SetMaintFields(donationCampaign);
                    await context.DonationCampaigns.AddAsync(donationCampaign);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding donation campaign {ex.Message}");
                throw;
            }
        }
    }

    private static async Task SeedDonations(IDbContextFactory<DataContext> contextFactory)
    {
        if (WebHelper.IsProduction())
        {
            Log.Logger.Information("Seed Donations Skipped since we are in Production");
            return;
        }

        try
        {
            Log.Logger.Information("SeedDonations Started");

            using (var context = contextFactory.CreateDbContext())
            {
                if (await context.Donations.AnyAsync()) return;

                List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
                List<Location> locations = await context.Locations.ToListAsync();
                List<DonationCampaign> donationCampaigns = await context.DonationCampaigns.ToListAsync();
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
                    var gross = new decimal(new Random().NextDouble() * 1000);
                    var fee = Math.Round(gross * .03m, 2);

                    Donation donation = new()
                    {
                        LocationId = location.LocationId,
                        DonationCampaignId = donationCampaigns.First(o => o.LocationId == location.LocationId).DonationCampaignId,
                        Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                        Gross = gross,
                        TransactionFee = fee,
                        TransactionId = new Random().Next(233999, 293737).ToString(),
                        FirstName = firstName,
                        LastName = lastName,
                        TaxFormSent = new Random().Next(2) == 0,
                        DonationDate = DateTime.UtcNow.AddDays((new Random().Next(364)) * -1),

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
        if (WebHelper.IsProduction())
        {
            Log.Logger.Information("Seed BedRequest Skipped since we are in Production");
            return;
        }

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
                        Team = (new Random().Next(1, 5)).ToString(),
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






using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Models;
using BedBrigade.Common;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Seeding;

public class Seed
{

    // Table User
    private const string _seedUserPassword = "Password";
    private const string _seedUserLocation = "Location";
    private const string _seedUserLocation1 = "Location1";
    private const string _seedUserLocation2 = "Location2";


    private static List<Role> Roles = new()
    {
        new Role {Name = "National Admin"},
        new Role {Name = "National Editor"},
        new Role {Name = "Location Admin"},
        new Role {Name = "Location Editor"},
        new Role {Name = "Location Author"},
        new Role {Name = "Location Scheduler"},
        new Role {Name = "Location Contributor"},
        new Role {Name = "Location Treasurer"},
        new Role {Name = "Location Communications"},
    };

    private static List<Location> locations = new()
    {
        new Location {Name=SeedConstants.SeedNationalName, Route="/national", Address1="", Address2="", City="Columbus", State="Ohio", PostalCode=""},
        new Location {Name="Living Hope Church", Route="/newark", Address1="", Address2="", City="Newark", State="Ohio", PostalCode=""},
        new Location {Name="Rock City Polaris", Route="/rock", Address1="", Address2="", City="Rock City", State="Ohio", PostalCode=""},
        new Location {Name="Peace Lutheran", Route="/linden", Address1="", Address2="", City="Linden", State="Ohio", PostalCode=""},
        new Location {Name="Vinyard Church", Route="/Circleville", Address1="", Address2="", City="Circleville", State="Ohio", PostalCode=""},
        new Location {Name="Hardbarger Impact", Route="/lancaster", Address1="", Address2="", City="Lancaster", State="Ohio", PostalCode=""},
        new Location {Name="Upper Arlington Lutheran", Route="/Arlington", Address1="", Address2="", City="Arlington", State="Ohio", PostalCode=""},
        new Location {Name="Greensburg United Methodist", Route="/canton", Address1="", Address2="", City="Canton", State="Ohio", PostalCode=""}

    };

    private static readonly List<User> users = new()
    {
        new User {FirstName = _seedUserLocation, LastName = "Contributor", Role = "Location Contributor" },
        new User {FirstName = _seedUserLocation, LastName = "Author", Role = "Location Author" },
        new User {FirstName = _seedUserLocation, LastName = "Editor", Role = "Location Editor" },
        new User {FirstName = _seedUserLocation, LastName = "Scheduler", Role = "Location Scheduler" },
        new User {FirstName = _seedUserLocation, LastName = "Treasurer", Role = "Location Treasurer"},
        new User {FirstName = _seedUserLocation, LastName = "Communications", Role = "Location Communications"},
        new User {FirstName = _seedUserLocation, LastName = "Admin", Role = "Location Admin"},
        new User {FirstName =  SeedConstants.SeedNationalName, LastName = "Editor", Role = "National Editor"},
        new User {FirstName =  SeedConstants.SeedNationalName, LastName = "Admin", Role = "National Admin"},
        new User {FirstName = _seedUserLocation1, LastName = "Contributor", Role = "Location Contributor" },
        new User {FirstName = _seedUserLocation1, LastName = "Author", Role = "Location Author" },
        new User {FirstName = _seedUserLocation1, LastName = "Editor", Role = "Location Editor" },
        new User {FirstName = _seedUserLocation1, LastName = "Scheduler", Role = "Location Scheduler" },
        new User {FirstName = _seedUserLocation1, LastName = "Treasurer", Role = "Location Treasurer"},
        new User {FirstName = _seedUserLocation1, LastName = "Communications", Role = "Location Communications"},
        new User {FirstName = _seedUserLocation1, LastName = "Admin", Role = "Location Admin"},
        new User {FirstName = _seedUserLocation2, LastName = "Contributor", Role = "Location Contributor" },
        new User {FirstName = _seedUserLocation2, LastName = "Author", Role = "Location Author" },
        new User {FirstName = _seedUserLocation2, LastName = "Editor", Role = "Location Editor" },
        new User {FirstName = _seedUserLocation2, LastName = "Scheduler", Role = "Location Scheduler" },
        new User {FirstName = _seedUserLocation2, LastName = "Treasurer", Role = "Location Treasurer"},
        new User {FirstName = _seedUserLocation2, LastName = "Communications", Role = "Location Communications"},
        new User {FirstName = _seedUserLocation2, LastName = "Admin", Role = "Location Admin"},
    };
    static readonly List<User> Users = users;

    public static async Task SeedData(IDbContextFactory<DataContext> _contextFactory)
    {
        await SeedConfigurations(_contextFactory);
        await SeedLocations(_contextFactory);
        await SeedContentsLogic.SeedContents(_contextFactory);
        await SeedMedia(_contextFactory);
        await SeedRoles(_contextFactory);
        await SeedUser(_contextFactory);
        //await SeedUserRoles(_contextFactory);
        await SeedVolunteersFor(_contextFactory);
        await SeedVolunteers(_contextFactory);
        await SeedDonations(_contextFactory);
        await SeedBedRequests(_contextFactory);
    }


    private static async Task SeedConfigurations(IDbContextFactory<DataContext> _contextFactory)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedConfigurations Started");
            if (await context.Configurations.AnyAsync()) return;

            var configurations = new List<Configuration>
            {
                new()
                {
                    ConfigurationKey = "FromEmailAddress",
                    ConfigurationValue = "webmaster@bedbrigade.org",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = "HostName",
                    ConfigurationValue = "mail.bedbrigade.org",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = "Port",
                    ConfigurationValue = "8889",
                    Section = ConfigSection.Email
                },
                new()
                {
                    ConfigurationKey = "TokenExpiration",
                    ConfigurationValue = "24",
                    Section = ConfigSection.System
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = "AllowedFileExtensions",
                    ConfigurationValue = ".jpg, .png, .pdf, .webp",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = "AllowedVideoExtensions",
                    ConfigurationValue = ".mp4",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = "MediaFolder",
                    ConfigurationValue = "media",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = "MainMediaSubFolder",
                    ConfigurationValue = "national",
                    Section = ConfigSection.Media
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = "MaxFileSize",
                    ConfigurationValue = "104857600",
                    Section = ConfigSection.Media    
                },
                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = "MaxVideoSize",
                    ConfigurationValue = "262144000",
                    Section = ConfigSection.Media
                }
            };

            await context.Configurations.AddRangeAsync(configurations);
            await context.SaveChangesAsync();
        }
    }
    private static async Task SeedLocations(IDbContextFactory<DataContext> _contextFactory)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedLocations Started");
            if (await context.Locations.AnyAsync()) return;

            try
            {
                await context.Locations.AddRangeAsync(locations);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Information($"Configuration seed error: {ex.Message}");
                throw;
            }
        }
    }
    private static async Task SeedMedia(IDbContextFactory<DataContext> _contextFactory)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedMedia Started");
            try
            {
                if (!await context.Media.AnyAsync(m => m.FileName == "Logo")) // table Media does not have site logo
                {
                    // var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                    // add the first reciord in Media table with National Logo
                    context.Media.Add(new Media
                    {
                        LocationId = 1,
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
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedRoles Started");
            if (await context.Roles.AnyAsync()) return;
            try
            {
                await context.Roles.AddRangeAsync(Roles);
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
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedUser Started");
            foreach (var user in Users)
            {
                if (!await context.Users.AnyAsync(u => u.UserName == $"{user.FirstName}{user.LastName}"))
                {
                    try
                    {
                        SeedRoutines.CreatePasswordHash(_seedUserPassword, out byte[] passwordHash, out byte[] passwordSalt);
                        List<Location> locations = context.Locations.ToList();
                        var location = locations[new Random().Next(locations.Count)];
                        if (user.FirstName == "National")
                        {
                            location = locations.Single(l => l.Name == "National");
                        }

                        // Create the user
                        var newUser = new User
                        {
                            UserName = $"{user.FirstName}{user.LastName}",
                            LocationId = location.LocationId,
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
    private static async Task SeedUserRoles(IDbContextFactory<DataContext> _contextFactory)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedUserRoles Started");
            try
            {
                var users = context.Users.ToList();
                foreach (var user in users)
                {
                    var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == user.Role);
                    UserRole newUserRole = new UserRole
                    {
                        LocationId = user.LocationId,
                        RoleId = role.RoleId,
                        UserName = user.UserName
                    };
                    await context.AddAsync(newUserRole);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error SeedUserRoles {ex.Message}");
                throw;
            }
        }
    }
    private static async Task SeedVolunteersFor(IDbContextFactory<DataContext> _contextFactory)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedVolunteersFor Started");
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
    private static async Task SeedVolunteers(IDbContextFactory<DataContext> _contextFactory)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            Log.Logger.Information("SeedVolunteers Started");
            if (await context.Volunteers.AnyAsync()) return;

            List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
            List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
            List<bool> YesOrNo = new List<bool> { true, false };
            List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
            List<VolunteerFor> volunteersFor = context.VolunteersFor.ToList();
            List<Location> locations = context.Locations.ToList();
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
            using (var context = contextFactory.CreateDbContext())
            {
                Log.Logger.Information("SeedDonations Started");
                if (await context.Donations.AnyAsync()) return;

                List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
                List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
                List<bool> YesOrNo = new List<bool> { true, false };
                List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
                List<Location> locations = await context.Locations.ToListAsync();

                for (var i = 0; i < 100;)
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
            using (var context = contextFactory.CreateDbContext())
            {
                Log.Logger.Information("Seed BedRequest Started");
                if (await context.BedRequests.AnyAsync()) return;

                List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
                List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
                List<bool> YesOrNo = new List<bool> { true, false };
                List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };
                List<string> City = new List<string> { "Columbus", "Cleveland", "Cincinnati", "Canton", "Youngston", "Springfield", "Middletown", "Beavercreek" };
                List<string> StreetName = new List<string> { "E. Bella Ln", "W. Chandler Blvd", "25 St.", "4th Ave.", "G Ave.", "Indian Wells CT.", "N. Arizona" };
                List<Location> locations = await context.Locations.ToListAsync();

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






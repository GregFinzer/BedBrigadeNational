﻿using Microsoft.EntityFrameworkCore;
using BedBrigade.Data.Data.Seeding;
using BedBrigade.Data.Models;
using Serilog;

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
        new Location {Name="Bed Brigade Columbus", Route="/", Address1="", Address2="", City="Columbus", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Living Hope Church", Route="/newark", Address1="", Address2="", City="Newark", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Rock City Polaris", Route="/rock", Address1="", Address2="", City="Rock City", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Peace Lutheran", Route="/linden", Address1="", Address2="", City="Linden", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Vinyard Church Circleville", Route="/Circleville", Address1="", Address2="", City="Circleville", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Hardbarger Impact", Route="/lancaster", Address1="", Address2="", City="Lancaster", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Upper Arlington Lutheran Church", Route="/Arlington", Address1="", Address2="", City="Arlington", State="Ohio", PostalCode=""},
        new Location {Name="Bed Brigade Greensburg United Methodist Church", Route="/canton", Address1="", Address2="", City="Canton", State="Ohio", PostalCode=""}

    };

    private static readonly List<User> users = new()
    {
        new User { FirstName = _seedUserLocation, LastName = "Contributor", Role = "Location Contributor" },
        new User {FirstName = _seedUserLocation, LastName = "Author", Role = "Location Author" },
        new User {FirstName = _seedUserLocation, LastName = "Editor", Role = "Location Editor" },
        new User {FirstName = _seedUserLocation, LastName = "Scheduler", Role = "Location Scheduler" },
        new User {FirstName = _seedUserLocation, LastName = "Treasurer", Role = "Location Treasurer"},
        new User {FirstName = _seedUserLocation, LastName = "Communications", Role = "Location Communications"},
        new User {FirstName = _seedUserLocation, LastName = "Admin", Role = "Location Admin"},
        new User {FirstName =  SeedConstants.SeedLocationNational, LastName = "Editor", Role = "National Editor"},
        new User {FirstName =  SeedConstants.SeedLocationNational, LastName = "Admin", Role = "National Admin"},
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
    private readonly IDbContextFactory<DataContext> _contextFactory;

    public static async Task SeedData(IDbContextFactory<DataContext> _contextFactory)
    {
        await SeedConfigurations(_contextFactory);
        await SeedLocations(_contextFactory);
        await SeedContentsLogic.SeedContents(_contextFactory);
        //await SeedMedia(context);
        await SeedRoles(_contextFactory);
        await SeedUser(_contextFactory);
        await SeedUserRoles(_contextFactory);
        await SeedVolunteers(_contextFactory);
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
                },
                new()
                {
                    ConfigurationKey = "HostName",
                    ConfigurationValue = "mail.bedbrigade.org",
                },
                new()
                {ConfigurationKey = "Port",
                ConfigurationValue = "8889"},
                new()
                {
                    ConfigurationKey = "TokenExpiration",
                    ConfigurationValue = "24"
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

                        // Create the user
                        var newUser = new User
                        {
                            UserName = $"{user.FirstName}{user.LastName}",
                            LocationId = location.LocationId,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = $"{user.FirstName}.{user.LastName}@bedBrigade.org".ToLower(),
                            Phone = "(999) 999-9999",
                            Role = user.Role,
                            FkRole = context.Roles.FirstOrDefault(r => r.Name == user.Role).RoleId,
                            PasswordHash = passwordHash,
                            PasswordSalt = passwordSalt,
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
                    UserRole newUserRole = new()
                    {
                        LocationId = user.LocationId,
                        Role = await context.Roles.FirstOrDefaultAsync(r => r.Name == user.Role),
                        User = user
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
    private static async Task SeedVolunteers(IDbContextFactory<DataContext> _contextFactory)
    {
        using(var context = _contextFactory.CreateDbContext())
        { 
        Log.Logger.Information("SeedVolunteers Started");
        if (await context.Volunteers.AnyAsync()) return;

        List<string> FirstNames = new List<string> { "Mike", "Sam", "John", "Luke", "Betty", "Joan", "Sandra", "Elizabeth", "Greg", "Genava" };
        List<string> LastNames = new List<string> { "Smith", "Willams", "Henry", "Cobb", "McAlvy", "Jackson", "Tomkin", "Corey", "Whipple", "Forbrzo" };
        List<string> VolunteeringFor = new List<string> { "Bed Building", "Bed Delivery", "Event Planning", "New Option", "Other" };
        List<bool> YesOrNo = new List<bool> { true, false };
        List<string> EmailProviders = new List<string> { "outlook.com", "gmail.com", "yahoo.com", "comcast.com", "cox.com" };

            for (var i = 0; i <= 100; i++)
            {
                var firstName = FirstNames[new Random().Next(FirstNames.Count - 1)];
                var lastName = LastNames[new Random().Next(LastNames.Count - 1)];
                var firstThree = new Random().Next(291, 861);
                var nextThree = new Random().Next(200, 890);
                var lastFour = new Random().Next(1000, 9999);

                Volunteer volunteer = new()
                {
                    Location = locations[new Random().Next(locations.Count - 1)],
                    VolunteeringFor = VolunteeringFor[new Random().Next(VolunteeringFor.Count - 1)],
                    VolunteeringForDate = DateTime.Now.AddDays(new Random().Next(60)),
                    IHaveVolunteeredBefore = YesOrNo[new Random().Next(YesOrNo.Count - 1)],
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}@" + EmailProviders[new Random().Next(EmailProviders.Count - 1)],
                    Phone = $"({firstThree}) {nextThree}-{lastFour}",
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



  



}





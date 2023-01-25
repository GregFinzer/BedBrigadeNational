using BedBrigade.Shared;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace BedBrigade.Server.Data
{
    public class Seed
    {
        private const string _seedUserName = "Seed";
        private const string _seedLocationNational = "National";
        private const string _seedLocationOhio = "OhioColumbus";
        private const string _seedLocationArizona = "ArizonaPrescott";


        // Table User
        private const string _seedUserAdmin = "Administrator";
        private const string _seedUserFirstName = "Admin";
        private const string _seedUserLastName = "User";
        private const string _seedUserEmail = "admin.user@bedbrigade.org";
        private const string _seedUserPhone = "99999999999";
        private const string _seedUserRole = "National Admin";
        private const string _seedUserPassword = "Password";
        private const string _seedUserLocation = "Location";

        private static readonly List<User> users = new()
        {
            new User { FirstName = _seedUserLocation, LastName = "Contributor", Role = "_seeUserLocation Contributor"},
            new User {FirstName = _seedUserLocation, LastName = "Author", Role = "_seeUserLocation Author" },
            new User {FirstName = _seedUserLocation, LastName = "Editor", Role = "Location Editor" },
            new User {FirstName = _seedUserLocation, LastName = "Scheduler", Role = "Location Scheduler" },
            new User {FirstName = _seedUserLocation, LastName = "Treasurer", Role = "Location Treasurer"},
            new User {FirstName = _seedUserLocation, LastName = "Communications", Role = "Location Communications"},
            new User {FirstName = _seedUserLocation, LastName = "Admin", Role = "Location Admin"},
            new User {FirstName = "National", LastName = "Editor", Role = "National Editor"},
            new User {FirstName = "National", LastName = "Admin", Role = "National Admin"}

        };
        static readonly List<User> Users = users;

        public static async Task SeedData(DataContext context)
        {
            await SeedConfigurations(context);
            await SeedLocations(context);
            await SeedContents(context);
            await SeedMedia(context);
            await SeedUser(context);

        }

        private static string GetHtml(string fileName)
        {
            return File.ReadAllText($"./Data/SeedHtml/{fileName}");
        }

        private static async Task SeedUser(DataContext context)
        {
            foreach (var user in Users)
            {
                if (!context.Users.Any(u => u.UserName == $"{user.FirstName}{user.LastName}"))
                {
                    SeedRoutines.CreatePasswordHash(_seedUserPassword, out byte[] passwordHash, out byte[] passwordSalt);
                    var location = _seedLocationOhio;
                    var roleLocation = user.Role.Split(' ')[0];
                    if (roleLocation == "National")
                    {
                        location = _seedLocationNational;
                    }
                    context.Users.Add(new User
                    {
                        UserName = $"{user.FirstName}{user.LastName}",
                        Location = context.Locations.Single(l => l.Name == location),
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = $"{user.FirstName}.{user.LastName}@bedBrigade.org".ToLower(),
                        Phone = "(999) 999-9999",
                        Role = user.Role,
                        PasswordHash = passwordHash,
                        PasswordSalt = passwordSalt,
                    });
                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch (DbException ex)
                    {
                        Console.WriteLine($"SaveChanges Error {ex.Message}");
                    }
                }
            }
        }

        private static async Task SeedMedia(DataContext context)
        {

            if (!context.Media.Any(m => m.Name == "Logo"))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                context.Media.Add(new Media
                {
                    Location = location!,
                    Name = "logo",
                    MediaType = "png",
                    Path = "images/national",
                    AltText = "Bed Brigade National Logo",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName,
                });

            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedContents(DataContext context)
        {
            var header = "Header";
            if (!context.Content.Any(c => c.ContentType == header))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                var seedHtml = GetHtml("header.html");
                context.Content.Add(new Content
                {
                    Location = location!,
                    ContentType = header,
                    Name = header,
                    ContentHtml = seedHtml,
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName,
                }); ;
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedLocations(DataContext context)
        {
            var locations = new List<Location>
            {
                new() {
                    Name = _seedLocationNational,
                    Route = "/",
                    PostalCode = string.Empty
                },
                new() {
                    Name = _seedLocationOhio,
                    Route = "/ohio",
                    PostalCode = string.Empty
                },
                new() {
                    Name = _seedLocationArizona,
                    Route = "/arizona",
                    PostalCode = string.Empty
                }
            };
            var rec = context.Locations.ToList();
            if (rec.Count > 0)
                context.Locations.RemoveRange(locations);

            await context.Locations.AddRangeAsync(locations);
            await context.SaveChangesAsync();
        }

        private static async Task SeedConfigurations(DataContext context)
        {
            if (context.Configurations.Any()) return;

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
                ConfigurationValue = "8889"}
            };

            await context.Configurations.AddRangeAsync(configurations);
            await context.SaveChangesAsync();
        }
    }
}

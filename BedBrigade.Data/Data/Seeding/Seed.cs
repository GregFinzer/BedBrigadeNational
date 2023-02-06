using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using BedBrigade.Data.Models;

namespace BedBrigade.Data.Seeding
{
    public class Seed
    {
        private const string _seedUserName = "Seed";
        private const string _seedLocationNational = "National";
        private const string _seedLocationOhio = "OhioColumbus";
        private const string _seedLocationArizona = "ArizonaPrescott";

        //private static List<Location> locations = new()
        //{
        //    new Location {Name = "Bed Brigade Columbus", Address1="", Address2="", City="Columbus", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Living Hope Church", Address1="", Address2="", City="Newark", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Rock City Polaris", Address1="", Address2="", City="Rock City", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Peace Lutheran", Address1="", Address2="", City="Linden", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Vinyard Church Circleville", Address1="", Address2="", City="Circleville", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Hardbarger Impact", Address1="", Address2="", City="Lancaster", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Upper Arlington Lutheran Church", Address1="", Address2="", City="Arlington", State="Ohio", PostalCode=""},
        //    new Location {Name = "Bed Brigade Greensburg United Methodist Church", Address1="", Address2="", City="Canton", State="Ohio", PostalCode=""}

        //};


        // Table User
        private const string _seedUserAdmin = "Administrator";
        private const string _seedUserFirstName = "Admin";
        private const string _seedUserLastName = "User";
        private const string _seedUserEmail = "admin.user@bedbrigade.org";
        private const string _seedUserPhone = "99999999999";
        private const string _seedUserRole = "National Admin";
        private const string _seedUserPassword = "Password";
        private const string _seedUserLocation = "Location";

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
            new User { FirstName = _seedUserLocation, LastName = "Contributor", Role = "UserLocation Contributor"},
            new User {FirstName = _seedUserLocation, LastName = "Author", Role = "UserLocation Author" },
            new User {FirstName = _seedUserLocation, LastName = "Editor", Role = "Location Editor" },
            new User {FirstName = _seedUserLocation, LastName = "Scheduler", Role = "Location Scheduler" },
            new User {FirstName = _seedUserLocation, LastName = "Treasurer", Role = "Location Treasurer"},
            new User {FirstName = _seedUserLocation, LastName = "Communications", Role = "Location Communications"},
            new User {FirstName = _seedUserLocation, LastName = "Admin", Role = "Location Admin"},
            new User {FirstName =  _seedLocationNational, LastName = "Editor", Role = "National Editor"},
            new User {FirstName =  _seedLocationNational, LastName = "Admin", Role = "National Admin"}

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
            return File.ReadAllText($"./Data/Seeding/SeedHtml/{fileName}");
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
                    if (roleLocation == _seedLocationNational)
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

            if (!context.Media.Any(m => m.FileName == "Logo"))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _seedLocationNational);
                context.Media.Add(new Media
                {
                    Location = location!,
                    FileName = "logo",
                    MediaType = "png",
                    Path = "media/national",
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
            {
                context.Locations.RemoveRange(locations);
                context.SaveChangesAsync();
            }
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

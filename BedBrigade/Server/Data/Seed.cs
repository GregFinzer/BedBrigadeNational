using BedBrigade.Shared;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text;

namespace BedBrigade.Server.Data
{
    public class Seed
    {
        private const string _seedUserName = "Seed";

        // Table User
        private const string _seedUserAdmin = "Administrator";
        private const string _seedUserFirstName = "Admin";
        private const string _seedUserLastName = "User";
        private const string _seedUserEmail = "admin.user@bedbrigade.org";
        private const string _seedUserPhone = "99999999999";
        private const string _seedUserRole = "NationalAdmin";
        private const string _seedUserPassword = "Password";
        private const string _national = "National";

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
            if(!context.Users.Any(u => u.UserName == _seedUserAdmin))
            {
                SeedRoutines.CreatePasswordHash(_seedUserPassword, out byte[] passwordHash, out byte[] passwordSalt);
                context.Users.Add(new User
                {
                    UserName = _seedUserAdmin,
                    Location = context.Locations.Single(l => l.Name == _national),
                    FirstName = _seedUserFirstName,
                    LastName = _seedUserLastName,
                    Email = _seedUserEmail,
                    Phone = _seedUserPhone,
                    Role = _seedUserRole,
                    PasswordHash = Encoding.UTF8.GetString(passwordHash),
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName,

                });
                try
                {
                    await context.SaveChangesAsync();
                }
                catch(DbException ex)
                {
                    Console.WriteLine($"SaveChanges Error {ex.Message}");
                }
            }
        }

        private static async Task SeedMedia(DataContext context)
        {

            if (!context.Media.Any(m => m.Name == "Logo"))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _national);
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
                var location = await context.Locations.FirstAsync(l => l.Name == _national);
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
            if (!context.Locations.Any(l => l.Name == _national))
            {
                context.Locations.Add(new Location
                {
                    Name = _national,
                    Route = "/",
                    PostalCode = string.Empty,
                    CreateDate= DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName,
                });
            }

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
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName
                },
                new()
                {
                    ConfigurationKey = "HostName",
                    ConfigurationValue = "mail.bedbrigade.org",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName
                },
                new()
                {
                    ConfigurationKey = "Port",
                    ConfigurationValue = "8889",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName
                }
            };

            await context.Configurations.AddRangeAsync(configurations);
            await context.SaveChangesAsync();
        }
    }
}

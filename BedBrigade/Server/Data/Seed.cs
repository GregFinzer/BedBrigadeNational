using BedBrigade.Shared;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Server.Data
{
    public class Seed
    {
        private const string _seedUserName = "Seed";
        private const string _national = "National";

        public static async Task SeedData(DataContext context)
        {
            await SeedConfigurations(context);
            await SeedLocations(context);
            await SeedContents(context);
            await SeedMedia(context);
        }

        private static async Task SeedMedia(DataContext context)
        {

            if (!context.Media.Any(m => m.Name == "Logo"))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _national);
                context.Media.Add(new Media
                {
                    Location = location!,
                    Name = "Logo",
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
            if (!context.Content.Any(c => c.ContentType == "Header"))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == _national);
                context.Content.Add(new Content
                {
                    Location = location!,
                    ContentType = "Header",
                    Name = "Header",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    CreateUser = _seedUserName,
                    UpdateUser = _seedUserName,
                    MachineName = Environment.MachineName,
                });
            }

            await context.SaveChangesAsync();
        }

        private static async Task SeedLocations(DataContext context)
        {
            if (!context.Locations.Any(l => l.Name == _national))
            {
                context.Locations.Add(new Location
                {
                    Name = "National",
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

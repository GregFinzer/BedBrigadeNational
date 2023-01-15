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
                    ContentHtml = "<header class=\"fixed-top bg-white shadow-sm\">\r\n    <nav class=\"navbar navbar-expand-md navbar-dark\" id=\"mainNav\">\r\n        <div class=\"container\">\r\n            <a class=\"navbar-brand\" href=\"/\"><img src=\"/images/national/logo.png\" class=\"img-fluid\" alt=\"Bed Brigade National Logo\" /></a>\r\n            <button class=\"navbar-toggler navbar-toggler-right\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#navbarResponsive\" aria-controls=\"navbarResponsive\" aria-expanded=\"false\" aria-label=\"Toggle navigation\">Menu <i class=\"fas fa-bars ms-1\"></i></button>\r\n\r\n            <div class=\"collapse navbar-collapse\" id=\"navbarResponsive\">\r\n                <ul class=\"navbar-nav ms-auto mb-2 mb-lg-0 menu-main-menu menu\">\r\n                    <li class=\"nav-item active\">\r\n                        <a class=\"nav-link\" href=\"/\">Home</a>\r\n                    </li>\r\n                    <li class=\"nav-item\">\r\n                        <a class=\"nav-link\" href=\"/request-bed\">Request A Bed</a>\r\n                    </li>\r\n                    <li class=\"nav-item\">\r\n                        <a class=\"nav-link\" href=\"/volunteer\">Volunteer</a>\r\n                    </li>\r\n                    <li class=\"nav-item\">\r\n                        <a class=\"nav-link\" href=\"/donations\">Donate</a>\r\n                    </li>\r\n                    <li class=\"nav-item\">\r\n                        <a class=\"nav-link\" href=\"/stories\">Stories</a>\r\n                    </li>\r\n                    <li class=\"dropdown nav-item\">\r\n                        <a class=\"nav-link dropdown-toggle\" data-bs-toggle=\"dropdown\" href=\"javascript:void(0);\">About</a>\r\n                        <ul class=\"dropdown-menu\">\r\n                            <li class=\"nav-item\">\r\n                                <a class=\"dropdown-item\" href=\"/about-us\">About Us</a>\r\n                            </li>\r\n                            <li class=\"nav-item\">\r\n                                <a href=\"/assembly-instructions\">Assembly Instructions</a>\r\n                            </li>\r\n                            <li class=\"nav-item\">\r\n                                <a class=\"dropdown-item\" href=\"/contact-us\">Contact Us</a>\r\n                            </li>\r\n                            <li class=\"nav-item\">\r\n                                <a class=\"dropdown-item\" href=\"/history-of-bed-brigade\">History of Bed Brigade</a>\r\n                            </li>\r\n                            <li class=\"nav-item\">\r\n                                <a class=\"dropdown-item\" href=\"/partners\">Partners</a>\r\n                            </li>\r\n                            <li class=\"nav-item\">\r\n                                <a class=\"dropdown-item\" href=\"/news\">News</a>\r\n                            </li>\r\n                            <li class=\"nav-item\">\r\n                                <a href=\"/locations\">Locations</a>\r\n                            </li>\r\n                        </ul>\r\n                    </li>\r\n                </ul>\r\n            </div>\r\n        </div>\r\n    </nav>\r\n    <div class=\"head-mobile-view d-block d-md-none text-center ps-2 pe-2\">\r\n        <ul class=\"navbar-nav ms-auto mb-2 mb-lg-0 menu-mobile-menu menu\">\r\n            <li class=\"nav-item\">\r\n                <a class=\"nav-link\" href=\"/request-bed\">Request A Bed</a>\r\n            </li>\r\n            <li class=\"nav-item\">\r\n                <a class=\"nav-link\" href=\"/volunteer\">Volunteer</a>\r\n            </li>\r\n            <li class=\"nav-item\">\r\n                <a class=\"nav-link\" href=\"/donations\">Donate</a>\r\n            </li>\r\n        </ul>\r\n    </div>\r\n</header>",
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

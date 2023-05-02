using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using static BedBrigade.Common.Common;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedContentsLogic
    {
        public static async Task SeedContents(IDbContextFactory<DataContext> _contextFactory)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                await SeedCirclevilleHeader(context);
                await SeedHeader(context);
                await SeedFooter(context);
                await SeedHomePageBody(context);
                await SeedRequestBedBody(context);
                await SeedVolunteerBody(context);
                await SeedDonateBody(context);
                await SeedHistoryBody(context);
                await SeedLocationsBody(context);
                await SeedNewsBody(context);
                await SeedPartnerBody(context);
                await SeedAssemblyBody(context);
                await SeedAboutUsBody(context);
                await SeedStoriesBody(context);
                await SeedContactBody(context);
                await SeedNewPageBody(context);
            }
        }

        private static async Task SeedNewPageBody(DataContext context)
        {
            var name = "NewPage";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml($"{name}.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "New Page"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedContactBody(DataContext context)
        {
            var name = "Contact";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml($"{name}.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Contact Us"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedStoriesBody(DataContext context)
        {
            var name = "Stories";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Stories.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Stories"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedHistoryBody(DataContext context)
        {
            var name = "History";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("History-of-bed-brigade.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "History of Bed Brigade"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedLocationsBody(DataContext context)
        {
            var name = "Locations";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Locations.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Locations"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedNewsBody(DataContext context)
        {
            var name = "News";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("News.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "News"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedPartnerBody(DataContext context)
        {
            var name = "Partners";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Partners.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Partners"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedAssemblyBody(DataContext context)
        {
            var name = "Assembly";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Assembly.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Assembly Instructions"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedAboutUsBody(DataContext context)
        {
            var name = "About";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("About-us.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "About Us"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedDonateBody(DataContext context)
        {
            var name = "Donate";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Donate.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Donate To Beed Brigade"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedVolunteerBody(DataContext context)
        {
            var name = "Volunteer";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Volunteer.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Volunteer"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedRequestBedBody(DataContext context)
        {
            var name = "RequestBed";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("RequestBed.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight",
                    Title = "Request A Bed"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedHomePageBody(DataContext context)
        {
           var name = "Home";
           var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
           if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId))
            {
                var seedHtml = GetHtml("HomePageBody.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    LeftMediaId = "imageLeft",
                    MiddleMediaId = "imageMiddle",
                    RightMediaId = "imageRight"
                });
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static async Task SeedHeader(DataContext context)
        {
            var name = "Header";
            var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
            if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId))
            {
                var seedHtml = GetHtml("header.html");
                Content content = new Content
                {
                    LocationId = location.LocationId,
                    ContentType = ContentType.Header,
                    Name = name,
                    ContentHtml = seedHtml,
                    FooterMediaId = "imageFooter",
                    HeaderMediaId = "imageHeader",
                };

                context.Content.Add(content);
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }

        }

        private static async Task SeedCirclevilleHeader(DataContext context)
        {
            var name = "header";
            var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationCircleville);
            if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId))
            {
                var seedHtml = GetHtml("circleville-header.html");
                var content = new Content
                {
                    LocationId = location.LocationId,
                    ContentType = ContentType.Header,
                    Name = name,
                    ContentHtml = seedHtml,
                    FooterMediaId = "imageFooter",
                    HeaderMediaId = "imageHeader",
                };

                context.Content.Add(content);
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }
        private static async Task SeedFooter(DataContext context)
        {
            var name = "Footer";
            var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
            if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId))
            {
                var seedHtml = GetHtml("footer.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Footer,
                    Name = name,
                    ContentHtml = seedHtml
                });               
            }
            location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationCircleville);
            if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId))
            {
                var seedHtml = GetHtml("circleville-footer.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Footer,
                    Name = name,
                    ContentHtml = seedHtml
                });
            }

            try
            {
            await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {ex.Message}");
            }
        }

        private static string GetHtml(string fileName)
        {
            var html = File.ReadAllText($"../BedBrigade.Data/Data/Seeding/SeedHtml/{fileName}");
            return html;
        }
    }
}

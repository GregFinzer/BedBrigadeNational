using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedContentsLogic
    {
        public static async Task SeedContents(IDbContextFactory<DataContext> _contextFactory)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
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

            }
        }

        private static async Task SeedStoriesBody(DataContext context)
        {
            var name = "Stories";
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Stories.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("History-of-bed-brigade.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Locations.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("News.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Partners.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Assembly.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("About-us.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Donate.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("Volunteer.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            var name = "RequestBed";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("RequestBed.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Body";
            var name = "Home";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("HomePageBody.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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
            var contentType = "Header";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("header.html");
                Content content = new Content
                {
                    LocationId = location.LocationId,
                    ContentType = contentType,
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
            var contentType = "Footer";
            var name = "Footer";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNational);
                var seedHtml = GetHtml("footer.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = contentType,
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

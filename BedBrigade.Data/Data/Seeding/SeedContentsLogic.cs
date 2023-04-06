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
                    ContentHtml = seedHtml
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

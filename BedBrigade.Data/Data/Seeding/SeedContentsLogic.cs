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
            }
        }

        private static async Task SeedHeader(DataContext context)
        {
            var header = "Header";
            if (!await context.Content.AnyAsync(c => c.ContentType == header))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNationalName);
                var seedHtml = GetHtml("header.html");
                Content content = new Content
                {
                    LocationId = location.LocationId,
                    ContentType = header,
                    Name = header,
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
            var footer = "Footer";
            if (!await context.Content.AnyAsync(c => c.ContentType == footer))
            {
                var location = await context.Locations.FirstAsync(l => l.Name == SeedConstants.SeedLocationNationalName);
                var seedHtml = GetHtml("footer.html");
                context.Content.Add(new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = footer,
                    Name = footer,
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

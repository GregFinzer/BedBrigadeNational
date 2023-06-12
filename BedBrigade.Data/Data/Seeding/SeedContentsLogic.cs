using BedBrigade.Common;
using BedBrigade.Data.Models;
using Microsoft.EntityFrameworkCore;
using static BedBrigade.Common.Common;
using static BedBrigade.Common.Extensions;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedContentsLogic
    {
        public static async Task SeedContents(IDbContextFactory<DataContext> _contextFactory)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                // Seed the folder structure under wwwroot/media
                await SeedImages(context);

                // The following seed the content db
                await SeedHeader(context);
                await SeedFooter(context);
                await SeedAboutUsBody(context);
                await SeedHomePageBody(context);
                await SeedRequestBedBody(context);
                await SeedVolunteerBody(context);
                await SeedDonateBody(context);
                await SeedHistoryBody(context);
                await SeedLocationsBody(context);
                await SeedNewsBody(context);
                await SeedPartnerBody(context);
                await SeedAssemblyBody(context);
                await SeedStoriesBody(context);
                await SeedContactBody(context);
                await SeedNewPageBody(context);
            }
        }

        private static async Task SeedImages(DataContext context)
        { 
            foreach (var location in context.Locations)
            {
                var locationRoute = GetAppRoot(location.Route);
                if (!Directory.Exists(locationRoute + "/pages"))
                {
                    CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages", GetAppRoot(location.Route));
                }
            }
        }

        private static async Task SeedHeader(DataContext context)
        {
            var name = "Header";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    var seedHtml = GetHtml($"header{location.LocationId}.html");
                    Content content = new Content
                    {
                        LocationId = location.LocationId,
                        ContentType = ContentType.Header,
                        Name = name,
                        ContentHtml = seedHtml,
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
        }
        private static async Task SeedFooter(DataContext context)
        {
            var name = "Footer";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    var seedHtml = GetHtml($"footer{location.LocationId}.html");
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
        }
        private static async Task SeedHomePageBody(DataContext context)
        {
            var name = "Home";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    var seedHtml = GetHtml($"Home{location.LocationId}.html");
                    context.Content.Add(new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Home,
                        Name = name,
                        ContentHtml = seedHtml,
                        LeftMediaId = "",
                        MiddleMediaId = "",
                        RightMediaId = "",
                        HeaderMediaId = "imageHeader",
                        FooterMediaId = "imageFooter"
                    });
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }
        private static async Task SeedNewPageBody(DataContext context)
        {
            var name = "NewPage";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {

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
                    var leftPath = $"{location.Route}/pages/NewPage/Left";
                    var middlePath = $"{location.Route}/pages/NewPage/Middle";
                    var rightPath = $"{location.Route}/pages/NewPage/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedContactBody(DataContext context)
        {
            var name = "Contact";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedStoriesBody(DataContext context)
        {
            var name = "Stories";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedHistoryBody(DataContext context)
        {
            var name = "History";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    var seedHtml = GetHtml("History.html");
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();

                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedLocationsBody(DataContext context)
        {
            var name = "Locations";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}";
                    var middlePath = $"{location.Route}";
                    var rightPath = $"{location.Route}";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }
        private static async Task SeedNewsBody(DataContext context)
        {
            var name = "News";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedPartnerBody(DataContext context)
        {
            var name = "Partners";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedAssemblyBody(DataContext context)
        {
            var name = "Assembly";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }
        private static async Task SeedAboutUsBody(DataContext context)
        {
            var name = "AboutUs";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = GetHtml("About-us.html");
                //var location = await context.Locations.FirstAsync(l => l.Route.ToLower() == SeedConstants.SeedLocationNational);
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages", GetAppRoot(location.Route) );
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
        }

        private static async Task SeedDonateBody(DataContext context)
        {
            var name = "Donate";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedVolunteerBody(DataContext context)
        {
            var name = "Volunteer";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

        private static async Task SeedRequestBedBody(DataContext context)
        {
            var name = "RequestBed";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
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
                    var leftPath = $"{location.Route}/pages/{name}/Left";
                    var middlePath = $"{location.Route}/pages/{name}/Middle";
                    var rightPath = $"{location.Route}/pages/{name}/Right";
                    leftPath.CreateDirectory();
                    middlePath.CreateDirectory();
                    rightPath.CreateDirectory();
                    //CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/{name}", GetAppRoot(location.Route));
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
        }

    }
}

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
                await SeedAboutPage(context);
                await SeedHomePage(context);
                await SeedDonatePage(context);
                await SeedNationalHistoryPage(context);
                await SeedNationalLocations(context);
                await SeedGroveCityPartners(context);
                await SeedAssemblyInstructions(context);
            }
        }

        private static async Task SeedImages(DataContext context)
        { 
            string mediaPath = GetAppRoot(string.Empty);
            if (!Directory.Exists(mediaPath))
            {
                Directory.CreateDirectory(mediaPath);
            }

            if (Directory.GetDirectories(mediaPath).Length == 0)
            {
                CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages", mediaPath);
            }
        }

        private static async Task SeedHeader(DataContext context)
        {
            var name = "Header";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = GetHtml("Header.html");
                Content content = new Content
                {
                    LocationId = (int) LocationNumber.National,
                    ContentType = ContentType.Header,
                    Name = name,
                    ContentHtml = seedHtml,
                };

                context.Content.Add(content);

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
                var seedHtml = GetHtml($"Footer.html");
                context.Content.Add(new Content
                {
                    LocationId = (int)LocationNumber.National,
                    ContentType = ContentType.Footer,
                    Name = name,
                    ContentHtml = seedHtml
                });

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
        private static async Task SeedHomePage(DataContext context)
        {
            var name = "Home";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    var seedHtml = GetHtml($"Home.html");

                    switch (location.LocationId)
                    {
                        case (int)LocationNumber.National:
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade");
                            break;
                        case (int)LocationNumber.GroveCity:
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Grove City");
                            break;
                        case (int)LocationNumber.RockCity:
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Polaris");
                            break;
                    }

                    context.Content.Add(new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Home,
                        Name = name,
                        ContentHtml = seedHtml,
                        Title = name
                    });

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


        private static async Task SeedNationalHistoryPage(DataContext context)
        {
            var name = "History";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = GetHtml("History.html");
                context.Content.Add(new Content
                {
                    LocationId = (int)LocationNumber.National,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    Title = "History of Bed Brigade"
                });
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

        private static async Task SeedNationalLocations(DataContext context)
        {
            var name = "Locations";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = GetHtml("Locations.html");
                context.Content.Add(new Content
                {
                    LocationId = (int)LocationNumber.National,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    Title = "Locations"
                });

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



        private static async Task SeedGroveCityPartners(DataContext context)
        {
            var name = "Partners";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {

                var seedHtml = GetHtml("Partners.html");
                context.Content.Add(new Content
                {
                    LocationId = (int)LocationNumber.GroveCity,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    Title = "Partners"
                });

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

        private static async Task SeedAssemblyInstructions(DataContext context)
        {
            var name = "Assembly";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    //Assembly instructions are location specific
                    if (location.LocationId == (int)LocationNumber.National)
                    {
                        continue;
                    }

                    var seedHtml = GetHtml("Assembly.html");
                    context.Content.Add(new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Body,
                        Name = name,
                        ContentHtml = seedHtml,
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
        }
        private static async Task SeedAboutPage(DataContext context)
        {
            var name = "AboutUs";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = GetHtml("Aboutus.html");
                foreach (var location in context.Locations)
                {
                    context.Content.Add(new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Body,
                        Name = name,
                        ContentHtml = seedHtml,
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
        }

        private static async Task SeedDonatePage(DataContext context)
        {
            var name = "Donate";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in context.Locations)
                {
                    //We don't take donations at the national level
                    if (location.LocationId == (int)LocationNumber.National)
                        continue;

                    var seedHtml = GetHtml("Donate.html");
                    context.Content.Add(new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Body,
                        Name = name,
                        ContentHtml = seedHtml,
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
        }
    }
}

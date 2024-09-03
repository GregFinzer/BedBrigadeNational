using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Data.Seeding;

    public static class SeedContentsLogic
    {
        public static async Task SeedContents(IDbContextFactory<DataContext> _contextFactory)
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                // Seed the folder structure under wwwroot/media
                await SeedImages(context);

                List<Location> locations = await context.Locations.ToListAsync();

                // The following seed the content db
                await SeedHeader(context, locations);
                await SeedFooter(context, locations);
                await SeedAboutPage(context, locations);
                await SeedHomePage(context, locations);
                await SeedDonationsPage(context, locations);
                await SeedNationalHistoryPage(context);
                await SeedNationalLocations(context);
                await SeedAssemblyInstructions(context, locations);
                await SeedThreeRotatorPageTemplate(context);
                await SeedGroveCity(context);
            }
        }

        private static async Task SeedGroveCity(DataContext context)
        {
            Log.Logger.Information("SeedGroveCity Started");
            var location = await context.Locations.FirstOrDefaultAsync(l => l.LocationId == (int)LocationNumber.GroveCity);

            if (location == null)
            {
                Console.WriteLine($"Error cannot find location with id: " + LocationNumber.GroveCity);
                return;
            }

            await SeedContentItem(context, ContentType.Header, location, "Header", "GroveCityHeader.html");
            await SeedContentItem(context, ContentType.Home, location, "Home", "GroveCityHome.html");
            await SeedContentItem(context, ContentType.Body, location, "AboutUs", "GroveCityAboutUs.html");
            await SeedContentItem(context, ContentType.Body, location, "Donations", "GroveCityDonations.html");
            await SeedContentItem(context, ContentType.Body, location, "Assembly-Instructions", "GroveCityAssemblyInstructions.html");
            await SeedContentItem(context, ContentType.Body, location, "Partners", "GroveCityPartners.html");
            await SeedContentItem(context, ContentType.Body, location, "Calendar", "GroveCityCalendar.html");
            await SeedContentItem(context, ContentType.Body, location, "Inventory", "GroveCityInventory.html");
    }

        private static async Task SeedContentItem(DataContext context, 
            ContentType contentType, 
            Location location,
            string name, string seedHtmlName)
        {
            if (await context.Content.AnyAsync(c => c.LocationId == location.LocationId && c.Name == name))
            {
                return;
            }

            string seedHtml = WebHelper.GetHtml(seedHtmlName);

            seedHtml = seedHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
            seedHtml = seedHtml.Replace("%%LocationName%%", location.Name);

            var content = new Content
            {
                LocationId = location.LocationId,
                ContentType = contentType,
                Name = name,
                ContentHtml = seedHtml,
                Title = StringUtil.InsertSpaces(name.Replace("-", " "))
            };

            SeedRoutines.SetMaintFields(content);
            context.Content.Add(content);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in content {name} for location {location.Name}: {ex.Message}");
            }
        }
    

    private static async Task SeedImages(DataContext context)
        {
            Log.Logger.Information("SeedImages Started");

            string mediaPath = FileUtil.GetMediaDirectory(string.Empty);
            if (!Directory.Exists(mediaPath))
            {
                Directory.CreateDirectory(mediaPath);
            }

            if (Directory.GetFiles(mediaPath, "photosIcon16x16.png", SearchOption.AllDirectories).Length == 0)
            {
                string seedDirectory = Common.Logic.FileUtil.GetSeedingDirectory();
                FileUtil.CopyDirectory($"{seedDirectory}/SeedImages", mediaPath);
            }
        }

        private static async Task SeedHeader(DataContext context, List<Location> locations)
        {
            Log.Logger.Information("SeedHeader Started");

            var name = "Header";

            foreach (var location in locations)
            {
                if (location.LocationId == (int)LocationNumber.GroveCity)
                {
                    continue;
                }

            bool alreadyAdded =
                    await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId);
                if (alreadyAdded)
                    continue;

                string seedHtml;
                if (location.LocationId == (int)LocationNumber.National)
                {
                    seedHtml = WebHelper.GetHtml("Header.html");
                }
                else
                {
                    seedHtml = WebHelper.GetHtml("LocationHeader.html");
                }

                Content content = new Content
                {
                    LocationId = location.LocationId,
                    ContentType = ContentType.Header,
                    Name = name,
                    ContentHtml = seedHtml,
                };

                content.ContentHtml = content.ContentHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
                content.ContentHtml = content.ContentHtml.Replace("%%LocationName%%", location.Name);

                SeedRoutines.SetMaintFields(content);
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

        private static async Task SeedFooter(DataContext context, List<Location> locations)
        {
            Log.Logger.Information("SeedFooter Started");

            var name = "Footer";

            foreach (var location in locations)
            {
                bool alreadyAdded =
                    await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId);
                if (alreadyAdded)
                    continue;

                string seedHtml;
                if (location.LocationId == (int)LocationNumber.National)
                {
                    seedHtml = WebHelper.GetHtml($"Footer.html");
                }
                else
                {
                    seedHtml = WebHelper.GetHtml("LocationFooter.html");
                }

                var content = new Content
                {
                    LocationId = location.LocationId,
                    ContentType = ContentType.Footer,
                    Name = name,
                    ContentHtml = seedHtml
                };

                content.ContentHtml = content.ContentHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
                content.ContentHtml = content.ContentHtml.Replace("%%LocationName%%", location.Name);
                SeedRoutines.SetMaintFields(content);
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

        private static async Task SeedHomePage(DataContext context, List<Location> locations)
        {
            Log.Logger.Information("SeedHomePage Started");

            var name = "Home";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                foreach (var location in locations)
                {
                    if (location.LocationId == (int)LocationNumber.GroveCity)
                    {
                        continue;
                    }

                    string seedHtml;

                    switch (location.LocationId)
                    {
                        case (int)LocationNumber.National:
                            seedHtml = WebHelper.GetHtml("Home.html");
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade");
                            break;
                        case (int)LocationNumber.GroveCity:
                            seedHtml = WebHelper.GetHtml("LocationHome.html");
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Grove City");
                            break;
                        case (int)LocationNumber.RockCity:
                            seedHtml = WebHelper.GetHtml("LocationHome.html");
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Polaris");
                            break;
                        default:
                            seedHtml = WebHelper.GetHtml("LocationHome.html");
                            seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", $"The Bed Brigade of {location.Name}");
                            break;
                    }

                    var content = new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Home,
                        Name = name,
                        ContentHtml = seedHtml,
                        Title = name
                    };

                    content.ContentHtml = content.ContentHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
                    content.ContentHtml = content.ContentHtml.Replace("%%LocationName%%", location.Name);

                    SeedRoutines.SetMaintFields(content);
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
        }


        private static async Task SeedNationalHistoryPage(DataContext context)
        {
            Log.Logger.Information("SeedNationalHistoryPage Started");

            var name = "History";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = WebHelper.GetHtml("History.html");

                var content = new Content
                {
                    LocationId = (int)LocationNumber.National,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    Title = "History of Bed Brigade"
                };

                SeedRoutines.SetMaintFields(content);
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

        private static async Task SeedNationalLocations(DataContext context)
        {
            Log.Logger.Information("SeedNationalLocations Started");

            var name = "Locations";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                var seedHtml = WebHelper.GetHtml("Locations.html");
                var content = new Content
                {
                    LocationId = (int)LocationNumber.National,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    Title = "Locations"
                };

                SeedRoutines.SetMaintFields(content);
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





        private static async Task SeedAssemblyInstructions(DataContext context, List<Location> locations)
        {
            Log.Logger.Information("SeedAssemblyInstructions Started");

            var name = "Assembly-Instructions";

            foreach (var location in locations)
            {
                //Do not seed National.  Grove City has it's own
                if (location.LocationId == (int)LocationNumber.National 
                    || location.LocationId == (int) LocationNumber.GroveCity)
                {
                    continue;
                }
                await SeedContentItem(context, ContentType.Body, location, name, "LocationAssemblyInstructions.html");
            }
        }

        private static async Task SeedAboutPage(DataContext context, List<Location> locations)
        {
            Log.Logger.Information("SeedAboutPage Started");

            var name = "AboutUs";
            if (!await context.Content.AnyAsync(c => c.Name == name))
            {
                string seedHtml;
                    
                foreach (var location in locations)
                {
                    if (location.LocationId == (int)LocationNumber.GroveCity)
                    {
                        continue;
                    }

                if (location.LocationId == (int)LocationNumber.National)
                    {
                        seedHtml = WebHelper.GetHtml("AboutUs.html");
                    }
                    else
                    {
                        seedHtml = WebHelper.GetHtml("LocationAboutUs.html");
                    }

                    var content = new Content
                    {
                        LocationId = location.LocationId!,
                        ContentType = ContentType.Body,
                        Name = name,
                        ContentHtml = seedHtml,
                        Title = "About Us"
                    };

                    content.ContentHtml = content.ContentHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
                    content.ContentHtml = content.ContentHtml.Replace("%%LocationName%%", location.Name);

                    SeedRoutines.SetMaintFields(content);
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

        private static async Task SeedDonationsPage(DataContext context, List<Location> locations)
        {
            Log.Logger.Information("SeedDonatePage Started");

            var name = "Donations";

            foreach (var location in locations)
            {
                //We don't take donations at the national level
                if (location.LocationId == (int)LocationNumber.National
                    || location.LocationId == (int)LocationNumber.GroveCity)
                    continue;

                await SeedContentItem(context, ContentType.Body, location, name, "LocationDonations.html");
            }
        }

        private static async Task SeedThreeRotatorPageTemplate(DataContext context)
        {
            Log.Logger.Information("Seed ThreeRotatorPageTemplate Started");

            var name = "ThreeRotatorPageTemplate";
            if (!await context.Templates.AnyAsync(c => c.Name == name))
            {
                var seedHtml = WebHelper.GetHtml("ThreeRotatorPageTemplate.html");

                var content = new Template
                {
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                };
                SeedRoutines.SetMaintFields(content);
                context.Templates.Add(content);

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


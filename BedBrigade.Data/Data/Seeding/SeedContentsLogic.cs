using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Seeding;
using BedBrigade.SpeakIt;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Data.Seeding;

public static class SeedContentsLogic
{
    private static TranslateLogic _translateLogic = new TranslateLogic(null);

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
            await SeedNationalDonations(context);
            await SeedAssemblyInstructions(context, locations);
            await SeedThreeRotatorPageTemplate(context);
            await SeedGroveCity(context);
            await SeedPolaris(context);
            await SeedForm(context, locations, ContentType.DeliveryCheckList);
            await SeedForm(context, locations, ContentType.EmailTaxForm);
            await SeedForm(context, locations, ContentType.BedRequestConfirmationForm);
            await SeedForm(context, locations, ContentType.SignUpEmailConfirmationForm);
            await SeedForm(context, locations, ContentType.SignUpSmsConfirmationForm);
            await SeedForm(context, locations, ContentType.NewsletterForm);
            await SeedForm(context, locations, ContentType.ContactUsConfirmationForm);
            await SeedForm(context, locations, ContentType.ForgotPasswordForm);
        }
    }







    private static async Task SeedNationalDonations(DataContext context)
    {
        Log.Logger.Information("SeedNationalDonations Started");

        var name = "Donate";
        if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == Defaults.NationalLocationId))
        {
            var seedHtml = WebHelper.GetSeedingFile("Donate.html");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

            var content = new Content
            {
                LocationId = Defaults.NationalLocationId,
                ContentType = ContentType.Body,
                Name = name,
                ContentHtml = seedHtml,
                Title = "General Donations"
            };

            SeedRoutines.SetMaintFields(content);
            context.Content.Add(content);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Debug($"Error in content {ex.Message}");
            }
        }
    }

    private static async Task SeedGroveCity(DataContext context)
    {
        Log.Logger.Information("SeedGroveCity Started");
        var location = await context.Locations.FirstOrDefaultAsync(l => l.LocationId == Defaults.GroveCityLocationId);

        if (location == null)
        {
            Console.WriteLine($"Error cannot find location with id: " + Defaults.GroveCityLocationId);
            return;
        }

        await SeedContentItem(context, ContentType.Header, location, "Header", "GroveCityHeader.html");
        await SeedContentItem(context, ContentType.Home, location, "Home", "GroveCityHome.html");
        await SeedContentItem(context, ContentType.Body, location, "AboutUs", "GroveCityAboutUs.html");
        await SeedContentItem(context, ContentType.Body, location, "Donations", "GroveCityDonations.html");
        await SeedContentItem(context, ContentType.Body, location, "Assembly-Instructions", "GroveCityAssemblyInstructions.html");
        await SeedContentItem(context, ContentType.Body, location, "Partners", "GroveCityPartners.html");
        await SeedContentItem(context, ContentType.Body, location, "Inventory", "GroveCityInventory.html");
        await SeedContentItem(context, ContentType.Body, location, "History", "GroveCityHistory.html");
    }

    private static async Task SeedPolaris(DataContext context)
    {
        Log.Logger.Information("SeedPolaris Started");
        var location = await context.Locations.FirstOrDefaultAsync(l => l.LocationId == Defaults.PolarisLocationId);

        if (location == null)
        {
            Console.WriteLine($"Error cannot find location with id: " + Defaults.PolarisLocationId);
            return;
        }

        await SeedContentItem(context, ContentType.Header, location, "Header", "PolarisHeader.html");
        await SeedContentItem(context, ContentType.Footer, location, "Footer", "PolarisFooter.html");
        await SeedContentItem(context, ContentType.Home, location, "Home", "PolarisHome.html");
        await SeedContentItem(context, ContentType.Body, location, "AboutUs", "PolarisAboutUs.html");
        await SeedContentItem(context, ContentType.Body, location, "History", "PolarisHistory.html");
        await SeedContentItem(context, ContentType.Body, location, "Donations", "PolarisDonations.html");
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

        string seedHtml = WebHelper.GetSeedingFile(seedHtmlName);

        if (contentType != ContentType.DeliveryCheckList)
        {
            seedHtml = seedHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
            seedHtml = seedHtml.Replace("%%LocationName%%", location.Name);
            seedHtml = seedHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);
        }

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
            Log.Logger.Debug($"Error in content {name} for location {location.Name}: {ex.Message}");
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

        string logoPath = Path.Combine(mediaPath, "national", "logo.png");
        if (!File.Exists(logoPath))
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
            if (location.LocationId == Defaults.GroveCityLocationId
                || location.LocationId == Defaults.PolarisLocationId)
            {
                continue;
            }

            bool alreadyAdded =
                await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId);
            if (alreadyAdded)
                continue;

            string seedHtml;
            if (location.LocationId == Defaults.NationalLocationId)
            {
                seedHtml = WebHelper.GetSeedingFile("Header.html");
            }
            else
            {
                seedHtml = WebHelper.GetSeedingFile("LocationHeader.html");
            }

            seedHtml = seedHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
            seedHtml = seedHtml.Replace("%%LocationName%%", location.Name);
            seedHtml = seedHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");

            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

            Content content = new Content
            {
                LocationId = location.LocationId,
                ContentType = ContentType.Header,
                Name = name,
                ContentHtml = seedHtml,
            };



            SeedRoutines.SetMaintFields(content);
            context.Content.Add(content);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Debug        ($"Error in content {ex.Message}");    
            }
        }

    }

    private static async Task SeedFooter(DataContext context, List<Location> locations)
    {
        Log.Logger.Information("SeedFooter Started");

        var name = "Footer";

        foreach (var location in locations)
        {
            if (location.LocationId == Defaults.PolarisLocationId)
            {
                continue;
            }

            bool alreadyAdded =
                await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId);
            if (alreadyAdded)
                continue;

            string seedHtml;
            if (location.LocationId == Defaults.NationalLocationId)
            {
                seedHtml = WebHelper.GetSeedingFile($"Footer.html");
            }
            else
            {
                seedHtml = WebHelper.GetSeedingFile("LocationFooter.html");
            }

            seedHtml = seedHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
            seedHtml = seedHtml.Replace("%%LocationName%%", location.Name);
            seedHtml = seedHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");

            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

            var content = new Content
            {
                LocationId = location.LocationId,
                ContentType = ContentType.Footer,
                Name = name,
                ContentHtml = seedHtml
            };

            SeedRoutines.SetMaintFields(content);
            context.Content.Add(content);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Debug($"Error in content {ex.Message}");
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
                if (location.LocationId == Defaults.GroveCityLocationId 
                    || location.LocationId == Defaults.PolarisLocationId)
                {
                    continue;
                }

                string seedHtml;

                switch (location.LocationId)
                {
                    case Defaults.NationalLocationId:
                        seedHtml = WebHelper.GetSeedingFile("Home.html");
                        seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade");
                        break;
                    case Defaults.GroveCityLocationId:
                        seedHtml = WebHelper.GetSeedingFile("LocationHome.html");
                        seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Grove City");
                        break;
                    case Defaults.PolarisLocationId:
                        seedHtml = WebHelper.GetSeedingFile("LocationHome.html");
                        seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Polaris");
                        break;
                    default:
                        seedHtml = WebHelper.GetSeedingFile("LocationHome.html");
                        seedHtml = seedHtml.Replace("The Bed Brigade of Columbus",
                            $"The Bed Brigade of {location.Name}");
                        break;
                }

                seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

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
                content.ContentHtml = content.ContentHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");

                SeedRoutines.SetMaintFields(content);
                context.Content.Add(content);


                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger.Debug($"Error in content {ex.Message}");
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
            var seedHtml = WebHelper.GetSeedingFile("History.html");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

            var content = new Content
            {
                LocationId = Defaults.NationalLocationId,
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
                Log.Logger.Debug($"Error in content {ex.Message}");
            }
        }
    }

    private static async Task SeedNationalLocations(DataContext context)
    {
        Log.Logger.Information("SeedNationalLocations Started");

        var name = "Locations";
        if (!await context.Content.AnyAsync(c => c.Name == name))
        {
            var seedHtml = WebHelper.GetSeedingFile("Locations.html");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

            var content = new Content
            {
                LocationId = Defaults.NationalLocationId,
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
                Log.Logger.Debug($"Error in content {ex.Message}");
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
            if (location.LocationId == Defaults.NationalLocationId
                || location.LocationId == Defaults.GroveCityLocationId)
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
                if (location.LocationId == Defaults.GroveCityLocationId
                    || location.LocationId == Defaults.PolarisLocationId)
                {
                    continue;
                }

                if (location.LocationId == Defaults.NationalLocationId)
                {
                    seedHtml = WebHelper.GetSeedingFile("AboutUs.html");
                }
                else
                {
                    seedHtml = WebHelper.GetSeedingFile("LocationAboutUs.html");
                }

                seedHtml = seedHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
                seedHtml = seedHtml.Replace("%%LocationName%%", location.Name);
                seedHtml = seedHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");

                seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

                var content = new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.Body,
                    Name = name,
                    ContentHtml = seedHtml,
                    Title = "About Us"
                };


                SeedRoutines.SetMaintFields(content);
                context.Content.Add(content);
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Debug($"Error in content {ex.Message}");
            }
        }
    }

    private static async Task SeedDonationsPage(DataContext context, List<Location> locations)
    {
        Log.Logger.Information("SeedDonatePage Started");

        var name = "Donations";

        foreach (var location in locations)
        {
            //We have different pages for National, Grove City and Polaris
            if (location.LocationId == Defaults.NationalLocationId
                || location.LocationId == Defaults.GroveCityLocationId
                || location.LocationId == Defaults.PolarisLocationId)
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
            var seedHtml = WebHelper.GetSeedingFile("ThreeRotatorPageTemplate.html");

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
                Log.Logger.Debug($"Error in content {ex.Message}");
            }
        }
    }

    private static async Task SeedForm(DataContext context, List<Location> locations, ContentType contentType)
    {
        // Do not seed National - remove it from list
        var National = locations.Find(l => l.LocationId == Defaults.NationalLocationId);
        if (National != null)
        {
            locations.Remove(National);
        }

        Log.Logger.Information($"SeedForm Started for {contentType}");

        string name = contentType.ToString();

        string seedText;

        foreach (var location in locations)
        {
            if (await context.Content.AnyAsync(c => c.Name == name && c.LocationId == location.LocationId))
            {
                continue; // already seeded
            }

            string fileName = $"{name}.txt";

            seedText = WebHelper.GetSeedingFile(fileName);

            var content = new Content
            {
                LocationId = location.LocationId!,
                ContentType = contentType,
                Name = name,
                ContentHtml = seedText,
                Title = StringUtil.InsertSpaces(name)
            };
            SeedRoutines.SetMaintFields(content);
            context.Content.Add(content); // add row to Contents table 
        }

        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Logger.Debug($"Error in SeedForm content for {contentType} {ex.Message}");
        }

    }
}


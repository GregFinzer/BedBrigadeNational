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
            await SeedDeliveryCheckList(context, locations);
            await SeedTaxForm(context, locations);
            await SeedThreeRotatorPageTemplate(context);
            await SeedGroveCity(context);
            await SeedRockCityPolaris(context);
            await SeedBedRequestConfirmationForm(context, locations);
            await SeedSignUpEmailConfirmationForm(context, locations);
            await SeedSignUpSmsConfirmationForm(context, locations);
        }
    }

    private static async Task SeedSignUpSmsConfirmationForm(DataContext context, List<Location> locations)
    {
        // Do not seed National - remove it from list
        var National = locations.Find(l => l.LocationId == (int)LocationNumber.National);
        if (National != null)
        {
            locations.Remove(National);
        }

        Log.Logger.Information("SeedSignUpSmsConfirmationForm Started");

        string name = ContentType.SignUpSmsConfirmationForm.ToString();

        if (!await context.Content.AnyAsync(c => c.Name == name))
        {
            string seedText;

            foreach (var location in locations)
            {
                seedText = WebHelper.GetHtml("SignUpSmsConfirmationForm.txt");

                var content = new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.SignUpSmsConfirmationForm,
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
                Log.Logger.Debug($"Error in SeedSignUpSmsConfirmationForm content {ex.Message}");
            }
        }
    }

    private static async Task SeedSignUpEmailConfirmationForm(DataContext context, List<Location> locations)
    {
        // Do not seed National - remove it from list
        var National = locations.Find(l => l.LocationId == (int)LocationNumber.National);
        if (National != null)
        {
            locations.Remove(National);
        }

        Log.Logger.Information("SeedSignUpEmailConfirmationForm Started");

        string name = ContentType.SignUpEmailConfirmationForm.ToString();

        if (!await context.Content.AnyAsync(c => c.Name == name))
        {
            string seedText;

            foreach (var location in locations)
            {
                seedText = WebHelper.GetHtml("SignUpEmailConfirmationForm.txt");

                var content = new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.SignUpEmailConfirmationForm,
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
                Log.Logger.Debug($"Error in SeedSignUpEmailConfirmationForm content {ex.Message}");
            }
        }
    }

    private static async Task SeedNationalDonations(DataContext context)
    {
        Log.Logger.Information("SeedNationalDonations Started");

        var name = "Donations";
        if (!await context.Content.AnyAsync(c => c.Name == name && c.LocationId == (int) LocationNumber.National))
        {
            var seedHtml = WebHelper.GetHtml("Donations.html");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

            var content = new Content
            {
                LocationId = (int)LocationNumber.National,
                ContentType = ContentType.Body,
                Name = name,
                ContentHtml = seedHtml,
                Title = name
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
        await SeedContentItem(context, ContentType.Body, location, "History", "GroveCityHistory.html");
    }

    private static async Task SeedRockCityPolaris(DataContext context)
    {
        Log.Logger.Information("SeedRockCityPolaris Started");
        var location = await context.Locations.FirstOrDefaultAsync(l => l.LocationId == (int)LocationNumber.RockCityPolaris);

        if (location == null)
        {
            Console.WriteLine($"Error cannot find location with id: " + LocationNumber.RockCityPolaris);
            return;
        }

        await SeedContentItem(context, ContentType.Header, location, "Header", "RockCityPolarisHeader.html");
        await SeedContentItem(context, ContentType.Footer, location, "Footer", "RockCityPolarisFooter.html");
        await SeedContentItem(context, ContentType.Home, location, "Home", "RockCityPolarisHome.html");
        await SeedContentItem(context, ContentType.Body, location, "AboutUs", "RockCityPolarisAboutUs.html");
        //await SeedContentItem(context, ContentType.Body, location, "Donations", "GroveCityDonations.html");
        //await SeedContentItem(context, ContentType.Body, location, "Assembly-Instructions", "GroveCityAssemblyInstructions.html");
        //await SeedContentItem(context, ContentType.Body, location, "Partners", "GroveCityPartners.html");
        //await SeedContentItem(context, ContentType.Body, location, "Calendar", "GroveCityCalendar.html");
        //await SeedContentItem(context, ContentType.Body, location, "Inventory", "GroveCityInventory.html");
        //await SeedContentItem(context, ContentType.Body, location, "History", "GroveCityHistory.html");
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
            if (location.LocationId == (int)LocationNumber.GroveCity
                || location.LocationId == (int) LocationNumber.RockCityPolaris)
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
            if (location.LocationId == (int)LocationNumber.RockCityPolaris)
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
                seedHtml = WebHelper.GetHtml($"Footer.html");
            }
            else
            {
                seedHtml = WebHelper.GetHtml("LocationFooter.html");
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
                if (location.LocationId == (int)LocationNumber.GroveCity 
                    || location.LocationId == (int) LocationNumber.RockCityPolaris)
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
                    case (int)LocationNumber.RockCityPolaris:
                        seedHtml = WebHelper.GetHtml("LocationHome.html");
                        seedHtml = seedHtml.Replace("The Bed Brigade of Columbus", "The Bed Brigade of Polaris");
                        break;
                    default:
                        seedHtml = WebHelper.GetHtml("LocationHome.html");
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
            var seedHtml = WebHelper.GetHtml("History.html");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

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
            var seedHtml = WebHelper.GetHtml("Locations.html");
            seedHtml = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(seedHtml);

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
            if (location.LocationId == (int)LocationNumber.National
                || location.LocationId == (int)LocationNumber.GroveCity)
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
                if (location.LocationId == (int)LocationNumber.GroveCity
                    || location.LocationId == (int) LocationNumber.RockCityPolaris)
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
            //We don't take donations at the national level
            if (location.LocationId == (int)LocationNumber.National
                || location.LocationId == (int)LocationNumber.GroveCity)
                continue;

            await SeedContentItem(context, ContentType.Body, location, name, "LocationDonations.html");
        }
    }

    private static async Task SeedDeliveryCheckList(DataContext context, List<Location> locations)
    {
        // Do not seed National - remove it from list
        var National = locations.Find(l => l.LocationId == (int)LocationNumber.National);
        if (National != null)
        {
            locations.Remove(National);
        }

        Log.Logger.Information("SeedDeliveryCheckList Started");

        string name = ContentType.DeliveryCheckList.ToString();

        if (!await context.Content.AnyAsync(c => c.Name == name))
        {
            string seedHtml;

            foreach (var location in locations)
            {

                seedHtml = WebHelper.GetHtml("DeliveryCheckList.txt"); // plane text

                var content = new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.DeliveryCheckList,
                    Name = name,
                    ContentHtml = seedHtml,
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
                Log.Logger.Debug($"Error in DeliveryCheckList content {ex.Message}");
            }
        }
    } // Delivery Check List

    private static async Task SeedTaxForm(DataContext context, List<Location> locations)
    {
        // Do not seed National - remove it from list
        var National = locations.Find(l => l.LocationId == (int)LocationNumber.National);
        if (National != null)
        {
            locations.Remove(National);
        }

        Log.Logger.Information("SeedTaxForm Started");

        string name = ContentType.EmailTaxForm.ToString();

        if (!await context.Content.AnyAsync(c => c.Name == name))
        {
            string seedHtml;

            foreach (var location in locations)
            {

                seedHtml = WebHelper.GetHtml("EmailTaxForm.txt"); 

                var content = new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.EmailTaxForm,
                    Name = name,
                    ContentHtml = seedHtml,
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
                Log.Logger.Debug($"Error in SeedTaxForm content {ex.Message}");
            }
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
                Log.Logger.Debug($"Error in content {ex.Message}");
            }
        }
    }

    private static async Task SeedBedRequestConfirmationForm(DataContext context, List<Location> locations)
    {
        // Do not seed National - remove it from list
        var National = locations.Find(l => l.LocationId == (int)LocationNumber.National);
        if (National != null)
        {
            locations.Remove(National);
        }

        Log.Logger.Information("SeedBedRequestConfirmationForm Started");

        string name = ContentType.BedRequestConfirmationForm.ToString();

        if (!await context.Content.AnyAsync(c => c.Name == name))
        {
            string seedText;

            foreach (var location in locations)
            {
                seedText = WebHelper.GetHtml("BedRequestConfirmationForm.txt");

                var content = new Content
                {
                    LocationId = location.LocationId!,
                    ContentType = ContentType.BedRequestConfirmationForm,
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
                Log.Logger.Debug($"Error in SeedBedRequestConfirmationForm content {ex.Message}");
            }
        }
    }
}


using System.Net;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.SpeakIt;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedNewsLogic
    {
        private static TranslateLogic _translateLogic = new TranslateLogic(null);

        public static async Task SeedNews(IDbContextFactory<DataContext> _contextFactory)
        {
            Log.Logger.Information("SeedNews Started");
            using (var context = _contextFactory.CreateDbContext())
            {
                if ((await context.Content.AnyAsync(c => c.ContentType == ContentType.News)))
                {
                    return;
                }

                SeedNationalNews(context);
                SeedGroveCityNews(context);

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger.Debug($"Error in SeedNews {ex.Message}");
                }
            }
        }

        private static void SeedGroveCityNews(DataContext context)
        {
            SeedCommunityLeadershipAward(context);
            SeedNewWebsite(context);
            SeedMeetThePresident(context);
        }

        private static void SeedNewWebsite(DataContext context)
        {
            DateTime createDate = DateUtil.AddTimeToExistingDate(new DateTime(2021, 11, 15), DateTime.UtcNow);

            Content content = new Content
            {
                LocationId = Defaults.GroveCityLocationId,
                ContentType = ContentType.News,
                Name = "New-Bed-Brigade-Columbus-Website",
                Title = "New Bed Brigade Columbus Website",
                ContentHtml = WebHelper.GetSeedingFile("News-New-Bed-Brigade-Columbus-Website.html"),
                MainImageFileName = "BedBrigadeColumbusWebsite.png",
                CreateDate = createDate,
                UpdateDate = createDate,
                CreateUser = "GregFinzer",
                UpdateUser = "GregFinzer",
            };
            CreateThumbnail(content);
            context.Content.Add(content);
        }

        private static void SeedMeetThePresident(DataContext context)
        {
            DateTime createDate = DateUtil.AddTimeToExistingDate(new DateTime(2024, 5, 28), DateTime.UtcNow);

            Content content = new Content
            {
                LocationId = Defaults.GroveCityLocationId,
                ContentType = ContentType.News,
                Name = "Meet-The-President",
                Title = "Meet The President",
                ContentHtml = WebHelper.GetSeedingFile("News-Meet-The-President.html"),
                MainImageFileName = "GregFinzer.jpg",
                CreateDate = createDate,
                UpdateDate = createDate,
                CreateUser = "GregFinzer",
                UpdateUser = "GregFinzer",
            };
            CreateThumbnail(content);
            context.Content.Add(content);
        }

        private static void SeedCommunityLeadershipAward(DataContext context)
        {
            DateTime createDate = DateUtil.AddTimeToExistingDate(new DateTime(2020, 11, 11), DateTime.UtcNow);

            Content content = new Content
            {
                LocationId = Defaults.GroveCityLocationId,
                ContentType = ContentType.News,
                Name = "2020-Community-Leadership-Award",
                Title = "2020 Community Leadership Award",
                ContentHtml = WebHelper.GetSeedingFile("News-2020-Community-Leadership-Award.html"),
                MainImageFileName = "2020CommunityLeadershipAward.jpg",
                CreateDate = createDate,
                UpdateDate = createDate,
                CreateUser = "DeanHoover",
                UpdateUser = "DeanHoover",
            };
            CreateThumbnail(content);
            context.Content.Add(content);
        }

        private static void SeedNationalNews(DataContext context)
        {
            DateTime createDate = DateUtil.AddTimeToExistingDate(new DateTime(2025, 4, 12), DateTime.UtcNow);

            Content content = new Content
            {
                LocationId = Defaults.NationalLocationId,
                ContentType = ContentType.News,
                Name = "Bed-Brigade-Featured-on-Spectrum-1-News",
                Title = "Bed Brigade Featured on Spectrum 1 News",
                ContentHtml = WebHelper.GetSeedingFile("News-Bed-Brigade-Featured-on-Spectrum-1-News.html"),
                MainImageFileName = "Spectrum1News.jpg",
                CreateDate = createDate,
                UpdateDate = createDate,
                CreateUser = "GregFinzer",
                UpdateUser = "GregFinzer",
            };
            CreateThumbnail(content);
            context.Content.Add(content);
        }

        private static void CreateThumbnail(Content content)
        {
            if (string.IsNullOrEmpty(content.MainImageFileName))
            {
                return;
            }

            //We are currently only seeding for Grove City and National locations for the News
            string locationName = content.LocationId == Defaults.NationalLocationId ? "National" : "Grove-City";
            string directory = Path.Combine(FileUtil.GetSeedingDirectory(), "SeedImages", locationName, "News", content.Name);
            string source = Path.Combine(directory, content.MainImageFileName);
            string destination = Path.Combine(directory, ImageUtil.GetThumbnailFileName(source));

            if (!File.Exists(destination))
            {
                ImageUtil.CreateThumbnail(source, destination, 600);
            }
        }
    }
}
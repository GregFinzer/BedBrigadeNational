using System.Text;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using CsvReader = BedBrigade.Common.Logic.CsvReader;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedStoriesLogic
    {
        public static async Task SeedStories(IDbContextFactory<DataContext> _contextFactory)
        {
            Log.Logger.Information("SeedStories Started");
            using (var context = _contextFactory.CreateDbContext())
            {
                if ((await context.Content.AnyAsync(c => c.ContentType == ContentType.Stories)))
                {
                    return;
                }

                CsvReader csvReader = new CsvReader();
                string path = Path.Combine(FileUtil.GetSeedingDirectory(), "SeedHtml", "StoriesData.csv");
                List<Dictionary<string, string>> stories = csvReader.CsvFileToDictionary(path);
                DateTime currentTime = DateTime.UtcNow;

                foreach (var storyContent in stories)
                {
                    CreateThumbnail(storyContent);
                    AddStory(context, storyContent, currentTime);
                    currentTime = currentTime.AddMinutes(1); 
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Log.Logger.Debug($"Error in SeedStories {ex.Message}");
                }
            }
        }

        private static void CreateThumbnail(Dictionary<string, string> storyContent)
        {
            string mainImageFileName = storyContent["MainImage"];
            string directory = Path.Combine(FileUtil.GetSeedingDirectory(), "SeedImages", "grove-city", "Stories",
                storyContent["Name"]);
            string source = Path.Combine(directory, mainImageFileName);
            string destination = Path.Combine(directory, ImageUtil.GetThumbnailFileName(source));

            if (!File.Exists(destination))
            {
                ImageUtil.CreateThumbnail(source, destination, 600);
            }
        }

        private static void AddStory(DataContext context, Dictionary<string, string> storyContent, DateTime currentTime)
        {
            DateTime createDate = DateTime.Parse(storyContent["CreateDate"]);
            createDate = DateUtil.AddTimeToExistingDate(createDate, currentTime);

            var content = new Content
            {
                LocationId = Defaults.GroveCityLocationId,
                ContentType = ContentType.Stories,
                Name = storyContent["Name"],
                Title = storyContent["Title"],
                ContentHtml = BuildHtml(storyContent),
                MainImageFileName = storyContent["MainImage"],
                CreateDate = createDate,
                UpdateDate = createDate,
                CreateUser = storyContent["CreateUser"],
                UpdateUser = storyContent["CreateUser"],
            };

            context.Content.Add(content);
        }

        private static string? BuildHtml(Dictionary<string, string> storyContent)
        {
            string description = storyContent["Description"];
            StringBuilder sb = new StringBuilder(description.Length * 2);
            sb.AppendLine("<p>");
            sb.AppendLine(description);
            sb.AppendLine("</p>");

            if (storyContent.ContainsKey("OtherImages"))
            {
                string[] images = storyContent["OtherImages"]
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var image in images)
                {
                    string imagePath = $"media/grove-city/Stories/{storyContent["Name"]}/{image}";

                    sb.AppendLine($"<img src=\"{imagePath}\">");
                    sb.AppendLine("<br />");
                }
            }

            return sb.ToString();
        }
    }
}

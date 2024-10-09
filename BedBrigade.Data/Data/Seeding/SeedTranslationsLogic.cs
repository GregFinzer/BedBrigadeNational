using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Seeding;
using BedBrigade.SpeakIt;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedTranslationsLogic
    {
        private static readonly ParseLogic parseLogic = new ParseLogic();
        private static readonly TranslateLogic _translateLogic = new TranslateLogic(null);

        public static async Task SeedContentTranslations(IDbContextFactory<DataContext> _contextFactory)
        {
            using (DataContext context = _contextFactory.CreateDbContext())
            {
                if (await context.ContentTranslations.AnyAsync()) return;

                List<Translation> translations = await context.Translations.ToListAsync();
                Dictionary<string, List<Translation>> translationsDictionary = _translateLogic.TranslationsToDictionary(translations);

                //National Pages
                await SeedContentTranslation(context, "Header", Defaults.NationalLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Footer", Defaults.NationalLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Home", Defaults.NationalLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "AboutUs", Defaults.NationalLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Donations", Defaults.NationalLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "History", Defaults.NationalLocationId, "es-MX", translationsDictionary);

                //Grove City Pages
                await SeedContentTranslation(context, "Header", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Footer", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Home", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "AboutUs", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Donations", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "History", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Assembly-Instructions", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Partners", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Calendar", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
                await SeedContentTranslation(context, "Inventory", Defaults.GroveCityLocationId, "es-MX", translationsDictionary);
            }
        }

        private static async Task SeedContentTranslation(DataContext context, 
            string name, 
            int locationId,
            string culture,
            Dictionary<string, List<Translation>> translations)
        {
            Content content = await context.Content.FirstOrDefaultAsync(c => c.Name == name && c.LocationId == locationId);

            if (content == null)
            {
                throw new Exception($"SeedContentTranslation Content '{name}' not found for location '{locationId}'");
            }

            string translated = _translateLogic.ParseAndTranslateText(content.ContentHtml, culture, translations);

            ContentTranslation contentTranslation = new ContentTranslation
            {
                LocationId = content.LocationId,
                ContentType = content.ContentType,
                Title = content.Title,
                Culture = culture,
                Name = content.Name,
                ContentHtml = translated
            };

            context.ContentTranslations.Add(contentTranslation);
            await context.SaveChangesAsync();
        }

        public static async Task SeedTranslationsAsync(IDbContextFactory<DataContext> _contextFactory)
        {
            using (DataContext context = _contextFactory.CreateDbContext())
            {
                if (await context.Translations.AnyAsync()) return;

                await SeedCultureTranslation(context, "en-US", null);
                List<Translation> englishContent = await context.Translations.ToListAsync();
                await SeedCultureTranslation(context, "es-MX", englishContent);
            }
        }

        private static async Task SeedCultureTranslation(DataContext context, string culture, List<Translation>? englishContent)
        {
            string fileName = culture + ".yml";
            string path = Path.Combine(FileUtil.GetSeedingDirectory(), "SeedTranslations", fileName);

            Dictionary<string, string> fileTranslations = parseLogic.ReadYamlFile(path);

            foreach (var fileTranslation in fileTranslations)
            {
                Translation translation = new Translation
                {
                    Hash = fileTranslation.Key,
                    Content = fileTranslation.Value,
                    Culture = culture
                };
                SeedRoutines.SetMaintFields(translation);

                if (englishContent != null)
                {
                    Translation? englishTranslation = englishContent.FirstOrDefault(t => t.Hash == fileTranslation.Key);
                    if (englishTranslation != null)
                    {
                        translation.ParentId = englishTranslation.TranslationId;
                    }
                }

                context.Translations.Add(translation);
            }

            await context.SaveChangesAsync();
        }
    }
}

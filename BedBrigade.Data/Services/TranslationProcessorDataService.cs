using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.SpeakIt;
using RestSharp;
using Serilog;
using JsonException = Newtonsoft.Json.JsonException;
using Translation = BedBrigade.Common.Models.Translation;

namespace BedBrigade.Data.Services
{
    public class TranslationProcessorDataService : ITranslationProcessorDataService
    {
        private readonly IConfigurationDataService _configurationDataService;
        private readonly IContentDataService _contentDataService;
        private readonly ITranslationDataService _translationDataService;
        private readonly IContentTranslationDataService _contentTranslationDataService;
        private readonly ITranslationQueueDataService _translationQueueDataService;
        private readonly IContentTranslationQueueDataService _contentTranslationQueueDataService;
        private readonly ITranslateLogic _translateLogic;
        private readonly ParseLogic _parseLogic = new ParseLogic();

        public TranslationProcessorDataService(IConfigurationDataService configurationDataService,
            IContentDataService contentDataService, 
            ITranslationDataService translationDataService,
            IContentTranslationDataService contentTranslationDataService,
            ITranslationQueueDataService translationQueueDataService,
            IContentTranslationQueueDataService contentTranslationQueueDataService,
            ITranslateLogic translateLogic)
        {
            _configurationDataService = configurationDataService;
            _contentDataService = contentDataService;
            _translationDataService = translationDataService;
            _contentTranslationDataService = contentTranslationDataService;
            _translationQueueDataService = translationQueueDataService;
            _contentTranslationQueueDataService = contentTranslationQueueDataService;
            _translateLogic = translateLogic;
        }

        public async Task ProcessQueue(CancellationToken cancellationToken)
        {
            await ProcessTranslations(cancellationToken);
            await ProcessContentTranslations(cancellationToken);
        }

        private async Task ProcessContentTranslations(CancellationToken cancellationToken)
        {
            var queueResult = await _translationQueueDataService.GetAllAsync();

            if (!queueResult.Success || queueResult.Data == null)
            {
                Log.Error("ProcessContentTranslations queueResult: " + queueResult.Message);
                return;
            }

            //If there are still translations to translate then return
            if (queueResult.Data.Any())
                return;

            var contentQueueResult = await _contentTranslationQueueDataService.GetAllAsync();

            if (!contentQueueResult.Success || contentQueueResult.Data == null)
            {
                Log.Error("ProcessContentTranslations contentQueueResult: " + contentQueueResult.Message);
                return;
            }

            var contentResult = await _contentDataService.GetAllAsync();

            if (!contentResult.Success || contentResult.Data == null)
            {
                Log.Error("ProcessContentTranslations contentResult: " + contentResult.Message);
                return;
            }

            var translationsResult = await _translationDataService.GetAllAsync();

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("ProcessContentTranslations translationsResult: " + translationsResult.Message);
                return;
            }


            var translationsDictionary = _translateLogic.TranslationsToDictionary(translationsResult.Data);
            foreach (var itemToProcess in contentQueueResult.Data)
            {
                await TranslateContentItem(contentResult, itemToProcess, translationsDictionary);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task TranslateContentItem(ServiceResponse<List<Content>> contentResult, ContentTranslationQueue itemToProcess,
            Dictionary<string, List<Translation>> translationsDictionary)
        {
            var parent = contentResult.Data.FirstOrDefault(o => o.ContentId == itemToProcess.ContentId);

            if (parent == null)
            {
                Log.Error("ProcessContentTranslations parent is null for ContentId " + itemToProcess.ContentId);
                return;
            }

            string cleaned = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(parent.ContentHtml);
            string translated = _translateLogic.ParseAndTranslateText(cleaned, itemToProcess.Culture, translationsDictionary);
                
            var existingResult = await _contentTranslationDataService.GetAsync(parent.Name, parent.LocationId, itemToProcess.Culture);

            if (existingResult.Success && existingResult.Data != null)
            {
                existingResult.Data.ContentHtml = translated;
                await _contentTranslationDataService.UpdateAsync(existingResult.Data);
            }
            else
            {
                ContentTranslation contentTranslation = new ContentTranslation
                {
                    LocationId = parent.LocationId,
                    ContentType = parent.ContentType,
                    Title = parent.Title,
                    Culture = itemToProcess.Culture,
                    Name = parent.Name,
                    ContentHtml = translated
                };
                await _contentTranslationDataService.CreateAsync(contentTranslation);
            }

            await _contentTranslationQueueDataService.DeleteAsync(itemToProcess.ContentTranslationQueueId);
        }

        private async Task ProcessTranslations(CancellationToken cancellationToken)
        {
            var translationsResult = await _translationDataService.GetTranslationsForLanguage(Defaults.DefaultLanguage);

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("ProcessTranslations translationsResult: " + translationsResult.Message);
                return;
            }

            var queueResult = await _translationQueueDataService.GetAllAsync();

            if (!queueResult.Success || queueResult.Data == null)
            {
                Log.Error("ProcessTranslations queueResult: " + queueResult.Message);
                return;
            }

            foreach (var itemToProcess in queueResult.Data)
            {
                var parentTranslation =
                    translationsResult.Data.FirstOrDefault(o => o.TranslationId == itemToProcess.TranslationId);

                if (parentTranslation == null)
                {
                    Log.Error(
                        $"ProcessTranslations parentTranslation is null for TranslationId {itemToProcess.TranslationId}");
                    continue;
                }

                var translatedText = await TranslateTextAsync(parentTranslation.Content, itemToProcess.Culture);

                if (translatedText == null)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Translation translation = new Translation
                {
                    Hash = parentTranslation.Hash,
                    Culture = itemToProcess.Culture,
                    Content = translatedText,
                    ParentId = parentTranslation.TranslationId
                };
                await _translationDataService.CreateAsync(translation);
                await _translationQueueDataService.DeleteAsync(itemToProcess.TranslationQueueId);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private async Task<string?> TranslateTextAsync(string textToTranslate, string cultureName)
        {
            try
            {
                string[] words = cultureName.Split("-");
                string locale = words[0];
                string url = $"https://api.apilayer.com/language_translation/translate?target={locale}";
                var client = new RestClient(url);

                CultureInfo cultureInfo = new CultureInfo(locale);

                Log.Debug($"Using: {url}");
                Log.Debug($"Translating to {cultureInfo.DisplayName}: {textToTranslate}");

                var request = new RestRequest();
                request.Method = Method.Post;

                var apiKey =
                    await _configurationDataService.GetConfigValueAsync(ConfigSection.System,
                        ConfigNames.TranslationApiKey);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Log.Error("TranslateTextAsync TranslationApiKey is null or empty");
                }

                request.AddHeader("apikey", apiKey);

                request.AddParameter("text/plain", textToTranslate, ParameterType.RequestBody);

                RestResponse response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    Log.Error("TranslateTextAsync response error " + response.ErrorMessage);
                    return null;
                }

                string translation = GetFirstTranslation(response.Content);

                Log.Debug($"Translated to: {translation}");
                return translation;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "TranslateTextAsync exception " + ex.Message);
                return null;
            }
        }

        private string? GetFirstTranslation(string? jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return null;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(jsonString);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("translations", out JsonElement translations) &&
                    translations.ValueKind == JsonValueKind.Array &&
                    translations.GetArrayLength() > 0)
                {
                    JsonElement firstTranslation = translations[0];
                    if (firstTranslation.TryGetProperty("translation", out JsonElement translationValue))
                    {
                        return translationValue.GetString();
                    }
                }

                return null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task QueueContentTranslation(Content content)
        {
            string html = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(content.ContentHtml);
            List<ParseResult> parseResults = _parseLogic.GetLocalizableStringsInText(html);
            List<string> distinctLocalizable = parseResults.Select(p => p.LocalizableString).ToList();
            distinctLocalizable.Add(content.Title);
            distinctLocalizable = distinctLocalizable.Distinct().ToList();

            await SaveNewEnglishLocalizableStringsToTranslations(distinctLocalizable);
            await QueueTranslationsForLocalizableStrings(distinctLocalizable);
            await QueueContentTranslationForNonEnglishCultures(content);
        }

        private async Task QueueContentTranslationForNonEnglishCultures(Content content)
        {
            var nonEnglishCultures =
                _translateLogic.GetRegisteredLanguages().Where(o => o.Name != Defaults.DefaultLanguage);

            foreach (var nonEnglishCulture in nonEnglishCultures)
            {
                //Queue the content translation
                ContentTranslationQueue contentTranslationQueue = new ContentTranslationQueue
                {
                    ContentId = content.ContentId,
                    Culture = nonEnglishCulture.Name
                };
                await _contentTranslationQueueDataService.CreateAsync(contentTranslationQueue);
            }
        }

        private async Task QueueTranslationsForLocalizableStrings(List<string> distinctLocalizable)
        {
            ServiceResponse<List<Translation>> translationsResult = await _translationDataService.GetAllAsync();

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("SaveNewEnglishLocalizableStringsToTranslations translationsResult: " +
                          translationsResult.Message);
                return;
            }

            ServiceResponse<List<TranslationQueue>> translationQueueResult =
                await _translationQueueDataService.GetAllAsync();

            if (!translationQueueResult.Success || translationQueueResult.Data == null)
            {
                Log.Error("SaveNewEnglishLocalizableStringsToTranslations translationQueueResult: " +
                          translationQueueResult.Message);
                return;
            }

            var nonEnglishCultures =
                _translateLogic.GetRegisteredLanguages().Where(o => o.Name != Defaults.DefaultLanguage);

            foreach (var nonEnglishCulture in nonEnglishCultures)
            {
                foreach (var localizableString in distinctLocalizable)
                {
                    var englishTranslation =
                        translationsResult.Data.FirstOrDefault(o => o.Culture == Defaults.DefaultLanguage && o.Content == localizableString);

                    if (englishTranslation == null)
                    {
                        Log.Error("SaveNewEnglishLocalizableStringsToTranslations englishTranslation is null for " +
                                  localizableString);
                        continue;
                    }

                    //Skip if the translation with the culture already exists
                    if (translationsResult.Data.Any(o => o.ParentId == englishTranslation.TranslationId && o.Culture == nonEnglishCulture.Name))
                    {
                        continue;
                    }

                    //Skip if the translation for the culture is already queued
                    if (translationQueueResult.Data.Any(o =>
                            o.Culture == nonEnglishCulture.Name && o.TranslationId == englishTranslation.TranslationId))
                    {
                        continue;
                    }

                    //Queue the translation
                    TranslationQueue translationQueue = new TranslationQueue
                    {
                        Culture = nonEnglishCulture.Name,
                        TranslationId = englishTranslation.TranslationId
                    };
                    await _translationQueueDataService.CreateAsync(translationQueue);
                }


            }
        }

        private async Task SaveNewEnglishLocalizableStringsToTranslations(List<string> distinctLocalizable)
        {
            bool itemsAdded = false;
            ServiceResponse<List<Translation>> translationsResult = await _translationDataService.GetAllAsync();

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("SaveNewEnglishLocalizableStringsToTranslations: " + translationsResult.Message);
                return;
            }

            List<Translation> defaultTranslations = translationsResult.Data.Where(t => t.Culture == Defaults.DefaultLanguage).ToList();

            foreach (var localizableString in distinctLocalizable)
            {
                if (!defaultTranslations.Any(o => o.Content == localizableString))
                {
                    string hash = _translateLogic.ComputeSHA512Hash(localizableString);
                    Translation translation = new Translation
                    {
                        Hash = hash,
                        Culture = Defaults.DefaultLanguage,
                        Content = localizableString
                    };
                    await _translationDataService.CreateAsync(translation);
                }
            }
        }
    }
}

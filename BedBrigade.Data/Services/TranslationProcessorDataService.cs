using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.SpeakIt;
using OpenAI;
using Serilog;
using System.Globalization;
using Translation = BedBrigade.Common.Models.Translation;
using System.Diagnostics;

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
        private int _maxPerMinute;
        private readonly Stopwatch _rateLimitStopwatch = new Stopwatch();
        private int _requestsThisMinute = 0;

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
            await LoadConfig();
            await ProcessTranslations(cancellationToken);
            await ProcessContentTranslations(cancellationToken);
        }

        private async Task LoadConfig()
        {
            _maxPerMinute = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.TranslationMaxRequestsPerMinute);
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

            // Ensure stopwatch is running
            if (!_rateLimitStopwatch.IsRunning)
            {
                _rateLimitStopwatch.Start();
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

                await RateLimiting(cancellationToken);

                var translatedText = await TranslateTextAsync(parentTranslation.Content, itemToProcess.Culture);
                _requestsThisMinute++;

                if (translatedText == null)
                {
                    break;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                Translation translation = BuildTranslation(parentTranslation, itemToProcess, translatedText);
                await _translationDataService.CreateAsync(translation);
                await _translationQueueDataService.DeleteAsync(itemToProcess.TranslationQueueId);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        private static Translation BuildTranslation(Translation parentTranslation, 
            TranslationQueue itemToProcess,
            string translatedText)
        {
            Translation translation = new Translation
            {
                Hash = parentTranslation.Hash,
                Culture = itemToProcess.Culture,
                Content = translatedText,
                ParentId = parentTranslation.TranslationId
            };
            return translation;
        }

        private async Task RateLimiting(CancellationToken cancellationToken)
        {
            if (_requestsThisMinute >= _maxPerMinute)
            {
                var elapsed = _rateLimitStopwatch.ElapsedMilliseconds;
                if (elapsed < 60000)
                {
                    var waitTime = 60000 - (int)elapsed;
                    Log.Information($"Translation Rate limit reached. Waiting {waitTime}ms before resuming.");
                    await Task.Delay(waitTime, cancellationToken);
                }

                // Reset for next cycle
                _requestsThisMinute = 0;
                _rateLimitStopwatch.Restart();
            }
        }


        private async Task<string?> TranslateTextAsync(string textToTranslate, string cultureName)
        {
            try
            {
                // Normalize culture name (e.g., "es-ES", "fr", etc.)
                var cultureInfo = new CultureInfo(cultureName);
                string targetLanguage = cultureInfo.DisplayName; // Human-readable language name

                // Get the API key from your configuration service
                var apiKey = await _configurationDataService.GetConfigValueAsync(
                    ConfigSection.System, ConfigNames.TranslationApiKey);

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    Log.Error("TranslateTextAsync TranslationApiKey is null or empty");
                    return null;
                }

                var client = new OpenAIClient(apiKey);

                // Use GPT-5 mini for translation
                var chat = client.GetChatClient("gpt-5-mini");

                string prompt =
                    $"Translate the following text from English into {targetLanguage}:\n\n{textToTranslate}";

                var response = await chat.CompleteChatAsync(prompt);
                string translation = response?.Value?.Content.FirstOrDefault()?.Text ?? string.Empty;

                if (String.IsNullOrEmpty(translation))
                {
                    Log.Error("TranslateTextAsync GPT response was null or empty");
                    return null;
                }

                Log.Debug($"Translated to {targetLanguage}: {translation}");

                return translation;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "TranslateTextAsync exception " + ex.Message);
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

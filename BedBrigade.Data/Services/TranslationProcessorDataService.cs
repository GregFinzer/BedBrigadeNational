using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Migrations;
using BedBrigade.SpeakIt;
using OpenAI;
using Serilog;
using System.Diagnostics;
using System.Globalization;
using Translation = BedBrigade.Common.Models.Translation;
using TranslationQueue = BedBrigade.Common.Models.TranslationQueue;

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
        private int _maxPerDay;
        private int _maxPerChunk;
        private readonly Stopwatch _rateLimitStopwatch = new Stopwatch();
        private int _requestsThisMinute = 0;
        private static int _requestsToday = 0;
        private int _lockWaitMinutes;
        private int _translationQueueKeepDays;
        private static DateTime _dayStart = DateTime.UtcNow.Date;

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
            if (await ProcessTranslations(cancellationToken))
            {
                await ProcessContentTranslations(cancellationToken);
            }
        }

        private async Task LoadConfig()
        {
            _maxPerMinute = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.TranslationMaxRequestsPerMinute);
            _maxPerDay = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.TranslationMaxRequestsPerDay);
            _maxPerChunk = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.TranslationMaxPerChunk);
            _lockWaitMinutes = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.TranslationLockWaitMinutes);
            _translationQueueKeepDays = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.System,
                ConfigNames.TranslationQueueKeepDays);
        }

        private async Task ProcessContentTranslations(CancellationToken cancellationToken)
        {
            if (await WaitForContentTranslationQueueLock())
                return;

            var contentTranslationQueueResult = await _contentTranslationQueueDataService.GetContentTranslationsToProcess(_maxPerChunk);
            Log.Debug("TranslationProcessorDataService:  ContentTranslationQueue: " + contentTranslationQueueResult.Count);

            if (!contentTranslationQueueResult.Any())
                return;

            //This should have less than 1000 records
            var contentResult = await _contentDataService.GetAllAsync();

            if (!contentResult.Success || contentResult.Data == null)
            {
                Log.Error("ProcessContentTranslations contentResult: " + contentResult.Message);
                return;
            }

            //This should have less than 5000 records (both the English source culture and the target cultures are needed)
            //We are always translating the cultures one after another 
            var translationsResult = await _translationDataService.GetAllAsync();

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("ProcessContentTranslations translationsResult: " + translationsResult.Message);
                return;
            }


            var translationsDictionary = _translateLogic.TranslationsToDictionary(translationsResult.Data);
            foreach (var itemToProcess in contentTranslationQueueResult)
            {
                await TranslateContentItem(contentResult, itemToProcess, translationsDictionary);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            await _contentTranslationQueueDataService.DeleteOldContentTranslationQueue(_translationQueueKeepDays);
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

            itemToProcess.LockDate = null;
            itemToProcess.SentDate = DateTime.UtcNow;
            itemToProcess.Status = QueueStatus.Sent.ToString();
            await _contentTranslationQueueDataService.UpdateAsync(itemToProcess);
        }

        private async Task<bool> WaitForTranslationQueueLock()
        {
            Log.Debug("TranslationProcessorDataService:  GetTranslationQueueLocked");
            List<TranslationQueue> lockedTranslations = await _translationQueueDataService.GetLockedTranslations();

            TranslationQueue firstLockedTranslation = lockedTranslations.FirstOrDefault();

            if (firstLockedTranslation != null)
            {
                //If there are translation queue locked less than an hour ago, skip
                if (firstLockedTranslation.LockDate.HasValue
                    && firstLockedTranslation.LockDate.Value > DateTime.UtcNow.AddMinutes(_lockWaitMinutes * -1))
                {
                    Log.Debug($"TranslationProcessorDataService:  TranslationQueue is currently locked, waiting for {_lockWaitMinutes} minutes");
                    return true;
                }

                //Clear locked because something bad happened and we want to retry
                await _translationQueueDataService.ClearTranslationQueueLock();
            }

            return false;
        }

        private async Task<bool> WaitForContentTranslationQueueLock()
        {
            Log.Debug("TranslationProcessorDataService:  ContentTranslationQueueLock");
            List<ContentTranslationQueue> lockedTranslations =
                await _contentTranslationQueueDataService.GetLockedContentTranslations();

            ContentTranslationQueue firstLockedTranslation = lockedTranslations.FirstOrDefault();

            if (firstLockedTranslation != null)
            {
                //If there are translation queue locked less than an hour ago, skip
                if (firstLockedTranslation.LockDate.HasValue
                    && firstLockedTranslation.LockDate.Value > DateTime.UtcNow.AddMinutes(_lockWaitMinutes * -1))
                {
                    Log.Debug($"TranslationProcessorDataService:  ContentTranslationQueue is currently locked, waiting for {_lockWaitMinutes} minutes");
                    return true;
                }

                //Clear locked because something bad happened and we want to retry
                await _contentTranslationQueueDataService.ClearContentTranslationQueueLock();
            }

            return false;
        }

        private async Task<bool> ProcessTranslations(CancellationToken cancellationToken)
        {
            if (await WaitForTranslationQueueLock())
                return false;

            var translationsToProcess = await _translationQueueDataService.GetTranslationsToProcess(_maxPerChunk);
            Log.Debug("TranslationProcessorDataService:  TranslationQueue: " + translationsToProcess.Count);

            //If there are no translations to translate then return
            if (!translationsToProcess.Any())
                return true; // It is not a failure, just nothing to do

            Log.Debug("TranslationProcessorDataService:  Locking TranslationQueue");
            await _translationQueueDataService.LockTranslationsToProcess(translationsToProcess);

            var translationsForLanguage = await _translationDataService.GetTranslationsForLanguage(Defaults.DefaultLanguage);

            if (!translationsForLanguage.Success || translationsForLanguage.Data == null)
            {
                Log.Error("ProcessTranslations translationsResult: " + translationsForLanguage.Message);
                return false;
            }

            // Ensure stopwatch is running
            if (!_rateLimitStopwatch.IsRunning)
            {
                _rateLimitStopwatch.Start();
            }

            foreach (var itemToProcess in translationsToProcess)
            {
                if (await ProcessTranslationItem(cancellationToken, translationsForLanguage.Data, itemToProcess))
                    continue;
                else
                    return false;
            }

            await _translationQueueDataService.DeleteOldTranslationQueue(_translationQueueKeepDays);
            return true;
        }

        private async Task<bool> ProcessTranslationItem(CancellationToken cancellationToken, 
            List<Translation> translations, 
            TranslationQueue translationQueue)
        {
            if (ReachedDailyLimit())
                return false;

            var parentTranslation =  translations.FirstOrDefault(o => o.TranslationId == translationQueue.TranslationId);

            if (parentTranslation == null)
            {
                Log.Error($"ProcessTranslationItem parentTranslation is null for TranslationId {translationQueue.TranslationId}");
                return true;
            }

            await RateLimiting(cancellationToken);

            var translatedText = await TranslateTextAsync(parentTranslation.Content, translationQueue.Culture);
            _requestsThisMinute++;
            _requestsToday++;

            if (translatedText == null)
            {
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            Translation translation = BuildTranslation(parentTranslation, translationQueue, translatedText);
            await _translationDataService.CreateAsync(translation);

            translationQueue.LockDate = null;
            translationQueue.SentDate = DateTime.UtcNow;
            translationQueue.Status = QueueStatus.Sent.ToString();
            await _translationQueueDataService.UpdateAsync(translationQueue);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            return true;
        }

        private bool ReachedDailyLimit()
        {
            // Reset daily counter if new day
            if (DateTime.UtcNow.Date > _dayStart)
            {
                _dayStart = DateTime.UtcNow.Date;
                _requestsToday = 0;
            }

            if (_requestsToday >= _maxPerDay)
            {
                Log.Information("Daily translation limit reached. Halting until next day.");
                return true;
            }

            return false;
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
                    $"Translate the following text from English into {targetLanguage}. Only give the best translation with no other options.:\n\n{textToTranslate}";

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
                await _contentTranslationQueueDataService.QueueContentTranslation(contentTranslationQueue);
            }
        }

        private async Task QueueTranslationsForLocalizableStrings(List<string> distinctLocalizable)
        {
            //This should have less than 5000 records (both the English source culture and the target cultures are needed)
            ServiceResponse<List<Translation>> translationsResult = await _translationDataService.GetAllAsync();

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("SaveNewEnglishLocalizableStringsToTranslations translationsResult: " +
                          translationsResult.Message);
                return;
            }

            //We are intentionally getting all queued translations to avoid queuing duplicates
            List<TranslationQueue> translationQueueResult =
                await _translationQueueDataService.GetTranslationsToProcess(Int32.MaxValue);

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
                    if (translationQueueResult.Any(o =>
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
                    await _translationQueueDataService.QueueTranslation(translationQueue);
                }
            }
        }

        private async Task SaveNewEnglishLocalizableStringsToTranslations(List<string> distinctLocalizable)
        {
            bool itemsAdded = false;
            ServiceResponse<List<Translation>> translationsResult = await _translationDataService.GetTranslationsForLanguage(Defaults.DefaultLanguage);

            if (!translationsResult.Success || translationsResult.Data == null)
            {
                Log.Error("SaveNewEnglishLocalizableStringsToTranslations: " + translationsResult.Message);
                return;
            }

            List<Translation> defaultTranslations = translationsResult.Data;

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

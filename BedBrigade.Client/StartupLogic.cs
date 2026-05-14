using AKSoftware.Localization.MultiLanguages;
using AKSoftware.Localization.MultiLanguages.Providers;
using BedBrigade.Client.Components;
using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Data;
using BedBrigade.Data.Seeding;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Syncfusion.Blazor;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.ResponseCompression;
using System;
using System.Data.SqlClient;

namespace BedBrigade.Client
{
    public static class StartupLogic
    {
        private const string DataProtectionApplicationName = "BedBrigadeNational";
        private const string DataProtectionKeysDirectoryName = "DataProtection-Keys";
        private const string DataProtectionKeysPathConfigKey = "DataProtection:KeysPath";
        private const string DataProtectionKeysPathEnvironmentVariable = "BedBrigadeDataProtectionKeysPath";
        private static ServiceProvider _svcProvider;

        public static void LoadConfiguration(WebApplicationBuilder builder)
        {
            if (Debugger.IsAttached || FileUtil.IsVSCodeInstalledOnWindows())
            {
                builder.Configuration.AddJsonFile("appsettings.Local.json", optional: false, reloadOnChange: true);
            }
            else if (Common.Logic.WebHelper.IsDevelopment())
            {
                builder.Configuration.AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true);
            }
            else if (Common.Logic.WebHelper.IsProduction())
            {
                builder.Configuration.AddJsonFile("appsettings.Production.json", optional: false, reloadOnChange: true);
            }
            else
            {
                throw new NotSupportedException($"Unknown Environment for Configuration: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
            }
        }

        public static void ConfigureLogger(WebApplicationBuilder builder)
        {
            //Read logging configuration from appsettings.json
            //See https://www.codeproject.com/Articles/5344667/Logging-with-Serilog-in-ASP-NET-Core-Web-API
            if (OperatingSystem.IsLinux())
            {
                Console.ForegroundColor = ResolveLinuxConsoleForegroundColor();
            }

            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext();

            if (OperatingSystem.IsLinux())
            {
                loggerConfiguration.WriteTo.Console(theme: SystemConsoleTheme.None);
            }
            else
            {
                loggerConfiguration.WriteTo.Console();
            }

            var logger = loggerConfiguration.CreateLogger();

            Log.Logger = logger;
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(logger);

            Log.Logger.Information("Starting Up");
        }

        public static void AddServicesToTheContainer(WebApplicationBuilder builder)
        {
            Log.Logger.Information("AddServicesToTheContainer");

            // Add services to the container.
            builder.Services.AddMvc(option => option.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddHttpContextAccessor();

            ConfigureDataProtection(builder);

            builder.Services.AddDbContextFactory<DataContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (OperatingSystem.IsLinux())
                {
                    var linuxConnectionString = Environment.GetEnvironmentVariable("BedBrigadeConnectionString");
                    if (!string.IsNullOrWhiteSpace(linuxConnectionString))
                    {
                        connectionString = linuxConnectionString;
                    }
                }
                
                if (!IsValidSqlServerConnectionString(connectionString))
                {
                    Log.Logger.Error("Invalid SQL Server connection string: " + connectionString);
                    throw new InvalidOperationException("Invalid SQL Server connection string. Please check the configuration. If you are running under Linux then set the environment variable BedBrigadeConnectionString with a valid connection string.");
                }
                
                options.UseSqlServer(connectionString, sqlBuilder =>
                {
                    sqlBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseApplicationServiceProvider(_svcProvider);
            });

            builder.Services.AddHttpClient();

            //Used to store the culture
            builder.Services.AddBlazoredLocalStorage(config =>
                config.JsonSerializerOptions.WriteIndented = true);
            
            SetupAuth(builder);
            ClientServices(builder);
            CommonLogic(builder);
            DataServices(builder);
            BackgroundServices(builder);

            builder.Services.AddScoped<ToastService, ToastService>();
            builder.Services.AddSignalR(o => { o.MaximumReceiveMessageSize = 300 * 1024 * 1024; });

            //Fix for AT&T Mobile Data
            builder.Services.AddResponseCompression(options =>
            {
                // Exclude SSE so it's never compressed
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes
                    .Except(new[] { "text/event-stream" });
            });
            
            builder.Services.AddServerSideBlazor().AddCircuitOptions(options => { options.DetailedErrors = true; });

            //services cors
            builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
            {
                builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));

            // Syncfusion Blazor Licensing
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);
            builder.Services.AddSyncfusionBlazor();

            _svcProvider = builder.Services.BuildServiceProvider();
        }

        private static void BackgroundServices(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<TranslationBackgroundService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<TranslationBackgroundService>());

            builder.Services.AddSingleton<EmailQueueBackgroundService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<EmailQueueBackgroundService>());

            builder.Services.AddSingleton<GeoLocationBackgroundService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<GeoLocationBackgroundService>());

            builder.Services.AddSingleton<SmsQueueBackgroundService>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<SmsQueueBackgroundService>());
        }

        private static void SetupAuth(WebApplicationBuilder builder)
        {
            Log.Logger.Information("SetupAuth");
            builder.Services.AddBlazoredSessionStorage();
            builder.Services.AddScoped<ICustomSessionService, CustomSessionService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IAuthDataService, AuthDataService>();
        }

        private static void ConfigureDataProtection(WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            Log.Logger.Information("ConfigureDataProtection");
            string keysPath = ResolveDataProtectionKeysPath(builder);

            try
            {
                DirectoryInfo keysDirectory = Directory.CreateDirectory(keysPath);
                builder.Services
                    .AddDataProtection()
                    .SetApplicationName(DataProtectionApplicationName)
                    .PersistKeysToFileSystem(keysDirectory);

                Log.Logger.Information("ASP.NET Core data protection keys persisted to {KeysPath}", keysDirectory.FullName);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Unable to initialize the ASP.NET Core data protection key directory at {KeysPath}", keysPath);
                throw;
            }
        }

        private static string ResolveDataProtectionKeysPath(WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            string? configuredPath = Environment.GetEnvironmentVariable(DataProtectionKeysPathEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                configuredPath = builder.Configuration[DataProtectionKeysPathConfigKey];
            }

            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                return NormalizeDataProtectionKeysPath(builder, configuredPath);
            }

            string parentDirectory = Directory.GetParent(builder.Environment.ContentRootPath)?.FullName
                ?? builder.Environment.ContentRootPath;
            return Path.GetFullPath(Path.Combine(parentDirectory, DataProtectionKeysDirectoryName));
        }

        private static string NormalizeDataProtectionKeysPath(WebApplicationBuilder builder, string configuredPath)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrWhiteSpace(configuredPath);

            string expandedPath = configuredPath.Trim();
            if (expandedPath == "~")
            {
                expandedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else if (expandedPath.StartsWith("~/", StringComparison.Ordinal))
            {
                string relativeToHome = expandedPath[2..].Replace('/', Path.DirectorySeparatorChar);
                expandedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), relativeToHome);
            }

            return Path.IsPathRooted(expandedPath)
                ? Path.GetFullPath(expandedPath)
                : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, expandedPath));
        }

        private static void CommonLogic(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IMailMergeLogic, MailMergeLogic>();
        }

        private static void ClientServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("ClientServices");
            builder.Services.AddSingleton<ICachingService, CachingService>();
            builder.Services.AddScoped<ILoadImagesService, LoadImagesService>();
            builder.Services.AddScoped<ILocationState, LocationState>();
            builder.Services.AddLanguageContainerForBlazorServer<EmbeddedResourceKeysProvider>(Assembly.GetExecutingAssembly(), "Resources");
            builder.Services.AddScoped<ILanguageService, LanguageService>();
            builder.Services.AddScoped<ICarouselService, CarouselService>();
            builder.Services.AddScoped<IScheduleControlService, ScheduleControlService>();
            builder.Services.AddScoped<IIFrameControlService, IFrameControlService>();
            builder.Services.AddScoped<IdleLogoutService>();
            builder.Services.AddScoped<IAdminMenuService, AdminMenuService>();
        }

        private static void DataServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("DataServices");
            builder.Services.AddScoped<ICommonService, CommonService>();
            builder.Services.AddScoped<IAuthDataService, AuthDataService>();
            builder.Services.AddScoped<IUserDataService, UserDataService>();
            builder.Services.AddScoped<ILocationDataService, LocationDataService>();
            builder.Services.AddScoped<IVolunteerDataService, VolunteerDataService>();
            builder.Services.AddScoped<IConfigurationDataService, ConfigurationDataService>();
            builder.Services.AddScoped<IContentDataService, ContentDataService>();
            builder.Services.AddScoped<IContentHistoryDataService, ContentHistoryDataService>();
            builder.Services.AddScoped<IVolunteerForDataService, VolunteerForDataService>();
            builder.Services.AddScoped<IDonationDataService, DonationDataService>();
            builder.Services.AddScoped<IBedRequestDataService, BedRequestDataService>();
            builder.Services.AddScoped<IScheduleDataService, ScheduleDataService>();
            builder.Services.AddScoped<ITemplateDataService, TemplateDataService>();
            builder.Services.AddScoped<IContactUsDataService, ContactUsDataService>();
            builder.Services.AddScoped<ISignUpDataService, SignUpDataService>();
            builder.Services.AddScoped<IEmailQueueDataService, EmailQueueDataService>();
            builder.Services.AddScoped<IMetroAreaDataService, MetroAreaDataService>();
            builder.Services.AddScoped<IHeaderMessageService, HeaderMessageService>();
            builder.Services.AddScoped<IUserPersistDataService, UserPersistDataService>();
            builder.Services.AddScoped<IDeliverySheetService, DeliverySheetService>();
            builder.Services.AddScoped<IMigrationDataService, MigrationDataService>();
            builder.Services.AddScoped<ITranslateLogic, TranslateLogic>();
            builder.Services.AddScoped<ITranslationDataService, TranslationDataService>();
            builder.Services.AddScoped<IContentTranslationDataService, ContentTranslationDataService>();
            builder.Services.AddScoped<ITranslationProcessorDataService, TranslationProcessorDataService>();
            builder.Services.AddScoped<ITranslationQueueDataService, TranslationQueueDataService>();
            builder.Services.AddScoped<IContentTranslationQueueDataService, ContentTranslationQueueDataService>();
            builder.Services.AddScoped<IEmailBuilderService, EmailBuilderService>();
            builder.Services.AddScoped<ISpokenLanguageDataService, SpokenLanguageDataService>();
            builder.Services.AddScoped<IGeoLocationQueueDataService, GeoLocationQueueDataService>();
            builder.Services.AddScoped<IGeoLocationProcessorDataService, GeoLocationProcessorDataService>();
            builder.Services.AddScoped<ISmsQueueDataService, SmsQueueDataService>();
            builder.Services.AddScoped<ISendSmsLogic, SendSmsLogic>();
            builder.Services.AddSingleton<ISmsState, SmsState>();
            builder.Services.AddScoped<ITimezoneDataService, TimezoneDataService>();
            builder.Services.AddScoped<INewsletterDataService, NewsletterDataService>();
            builder.Services.AddScoped<ISubscriptionDataService, SubscriptionDataService>();
            builder.Services.AddScoped<IDonationCampaignDataService, DonationCampaignDataService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IEmailBounceService, EmailBounceService>();
            builder.Services.AddScoped<IDashboardDataService, DashboardDataService>();
            builder.Services.AddScoped<IDeliveryPlanService, DeliveryPlanService>();
            builder.Services.AddScoped<ITeamSheetService, TeamSheetService>();
        }

        public static WebApplication CreateAndConfigureApplication(WebApplicationBuilder builder)
        {
            Log.Logger.Information("Create and configure application");
            var app = builder.Build();

            app.UsePathBase("/National");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseHttpsRedirection();

            //Possible fix for AT&T Mobile Data            
            app.UseResponseCompression();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.HasValue)
                {
                    string requestPath = context.Request.Path.Value;
                    string? resolvedPath = ResolveMediaRequestPath(app.Environment.WebRootPath, requestPath);
                    if (!string.IsNullOrWhiteSpace(resolvedPath))
                    {
                        context.Request.Path = new PathString(resolvedPath);
                    }
                }

                await next();
            });

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAntiforgery();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            return app;
        }

        public static async Task SetupDatabase(WebApplication app)
        {
            if (!await ShouldSeed(app))
            {
                return;
            }

            Log.Logger.Information("Setup Database");

            //Create database if it does not exist
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
                using (var context = dbContextFactory.CreateDbContext())
                {
                    Log.Logger.Information("Performing Migration");
                    await context.Database.MigrateAsync();
                }
                Log.Logger.Information("Seeding Data");
                await Seed.SeedData(dbContextFactory);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "An error occurred during migration");
                Environment.Exit(1);
            }

        }

        public static async Task<bool> ShouldSeed(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                //Setup database if it does not exist
                bool databaseExists = await context.Database.CanConnectAsync();

                if (!databaseExists)
                {
                    return true;
                }

                //Setup database if the Configurations table does not exist
                var tableExists = context.Database
                    .ExecuteSqlRaw(
                        @"SELECT CASE WHEN OBJECT_ID(N'dbo.Configurations', N'U') IS NOT NULL THEN 1 ELSE 0 END") == 1;

                if (!tableExists)
                {
                    return true;
                }
            }

            if (!Debugger.IsAttached)
            {
                Log.Logger.Information("Setup Database skipped because we are not in local development");
                return false;
            }

            string solutionDirectory = FileUtil.GetSolutionPath();
            string dataDirectory = Path.Combine(solutionDirectory, "BedBrigade.Data");

            //Do not set up the database if there were no data changed locally today and the database exists
            if (!FileUtil.AnyCSharpFilesModifiedToday(dataDirectory) 
                && !FileUtil.AnyHtmlFilesModifiedToday(dataDirectory))
            {
                Log.Logger.Information("Setup Database skipped because no .cs files have been modified today in " + dataDirectory);
                return false;
            }

            return true;
        }

        public static async Task SetupCaching(WebApplication app)
        {
            Log.Logger.Information("Setup Caching");
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                var config = await context.Configurations.FirstOrDefaultAsync( o => o.ConfigurationKey == ConfigNames.IsCachingEnabled
                    && o.LocationId == Defaults.NationalLocationId);
                if (config != null)
                {
                    if (config != null)
                    {
                        var cachingService = services.GetRequiredService<ICachingService>();
                        cachingService.IsCachingEnabled = true;
                        bool isCachingEnabled;

                        if (bool.TryParse(config.DecryptedValue, out isCachingEnabled))
                        {
                            cachingService.IsCachingEnabled = isCachingEnabled;
                        }

                        Log.Logger.Information(cachingService.IsCachingEnabled
                            ? "Caching is enabled"
                            : "Caching is disabled");
                    }
                }
            }
        }

        private static string? ResolveMediaRequestPath(string webRootPath, string requestPath)
        {
            if (string.IsNullOrWhiteSpace(webRootPath) || string.IsNullOrWhiteSpace(requestPath))
            {
                return null;
            }

            const string MediaPrefix = "/media";
            if (!requestPath.StartsWith(MediaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string remainder = requestPath.Substring(MediaPrefix.Length).TrimStart('/');
            string mediaRoot = Path.Combine(webRootPath, "Media");
            string combinedPath = Path.Combine(mediaRoot, remainder);
            string? physicalPath = FileUtil.ResolveCaseInsensitivePath(combinedPath);
            
            if (string.IsNullOrWhiteSpace(physicalPath))
            {
                return null;
            }

            string relativePath = Path.GetRelativePath(webRootPath, physicalPath).Replace('\\', '/');
            return relativePath.StartsWith("/", StringComparison.Ordinal)
                ? relativePath
                : "/" + relativePath;
        }



        private static bool IsValidSqlServerConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return false;

            try
            {
                var builder = new SqlConnectionStringBuilder(connectionString);

                // Optional: enforce required fields
                if (string.IsNullOrWhiteSpace(builder.DataSource))
                    return false;

                // Either Integrated Security OR UserID/Password should be present
                if (!builder.IntegratedSecurity)
                {
                    if (string.IsNullOrWhiteSpace(builder.UserID) ||
                        string.IsNullOrWhiteSpace(builder.Password))
                        return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false; // Invalid format
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static ConsoleColor ResolveLinuxConsoleForegroundColor()
        {
            string? colorFgBg = Environment.GetEnvironmentVariable("COLORFGBG");
            if (!string.IsNullOrWhiteSpace(colorFgBg))
            {
                string[] parts = colorFgBg.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length > 0 && int.TryParse(parts[^1], out int backgroundCode))
                {
                    // ANSI 0-6 is generally dark backgrounds, 7+ is generally light backgrounds.
                    return backgroundCode <= 6 ? ConsoleColor.White : ConsoleColor.Black;
                }
            }

            return IsDarkConsoleBackground(Console.BackgroundColor)
                ? ConsoleColor.White
                : ConsoleColor.Black;
        }

        private static bool IsDarkConsoleBackground(ConsoleColor backgroundColor)
        {
            return backgroundColor switch
            {
                ConsoleColor.Black => true,
                ConsoleColor.DarkBlue => true,
                ConsoleColor.DarkGreen => true,
                ConsoleColor.DarkCyan => true,
                ConsoleColor.DarkRed => true,
                ConsoleColor.DarkMagenta => true,
                ConsoleColor.DarkYellow => true,
                ConsoleColor.DarkGray => true,
                _ => false
            };
        }
    }
}

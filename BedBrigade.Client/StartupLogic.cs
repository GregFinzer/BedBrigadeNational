using BedBrigade.Client.Components;
using BedBrigade.Client.Services;
using BedBrigade.Common;
using BedBrigade.Data;
using BedBrigade.Data.Seeding;
using BedBrigade.Data.Services;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Syncfusion.Blazor;

namespace BedBrigade.Client
{
    public static class StartupLogic
    {
        private static ServiceProvider _svcProvider;

        public static void ConfigureLogger(WebApplicationBuilder builder)
        {
            //Read logging configuration from appsettings.json
            //See https://www.codeproject.com/Articles/5344667/Logging-with-Serilog-in-ASP-NET-Core-Web-API
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

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

            builder.Services.AddDbContextFactory<DataContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlBuilder =>
                {
                    sqlBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseApplicationServiceProvider(_svcProvider);
            });

            builder.Services.AddHttpClient();

            SetupAuth(builder);
            ClientServices(builder);
            DataServices(builder);

            builder.Services.AddScoped<ToastService, ToastService>();
            builder.Services.AddSignalR(e =>
            {
                e.MaximumReceiveMessageSize = 1024000;
            });

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

        private static void SetupAuth(WebApplicationBuilder builder)
        {
            Log.Logger.Information("SetupAuth");

            //Authentication
            builder.Services.AddAuthorization();

            //The cookie authentication is never used, but it is required to prevent a runtime error
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "auth_cookie";
                    options.Cookie.MaxAge = TimeSpan.FromHours(24);
                });

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddCascadingAuthenticationState();

            builder.Services.AddScoped<IAuthDataService, AuthDataService>();
            builder.Services.AddBlazoredSessionStorage();
            builder.Services.AddScoped<ICustomSessionService, CustomSessionService>();

        }

        private static void ClientServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("ClientServices");
            builder.Services.AddSingleton<ICachingService, CachingService>();
            builder.Services.AddScoped<ILoadImagesService, LoadImagesService>();
        }

        private static void DataServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("DataServices");
            builder.Services.AddScoped<ICommonService, CommonService>();
            builder.Services.AddScoped<IAuthDataService, AuthDataService>();
            builder.Services.AddScoped<IUserDataService, UserDataService>();
            builder.Services.AddScoped<IDonationDataService, DonationDataService>();
            builder.Services.AddScoped<ILocationDataService, LocationDataService>();
            builder.Services.AddScoped<IVolunteerDataService, VolunteerDataService>();
            builder.Services.AddScoped<IConfigurationDataService, ConfigurationDataService>();
            builder.Services.AddScoped<IContentDataService, ContentDataService>();
            builder.Services.AddScoped<IVolunteerForDataService, VolunteerForDataService>();
            builder.Services.AddScoped<IMediaDataService, MediaDataService>();
            builder.Services.AddScoped<IDonationDataService, DonationDataService>();
            builder.Services.AddScoped<IBedRequestDataService, BedRequestDataService>();
            builder.Services.AddScoped<IScheduleDataService, ScheduleDataService>();
            builder.Services.AddScoped<ITemplateDataService, TemplateDataService>();
            builder.Services.AddScoped<IContactUsDataService, ContactUsDataService>();
            builder.Services.AddScoped<IVolunteerEventsDataService, VolunteerEventsDataService>();
            builder.Services.AddScoped<IEmailQueueDataService, EmailQueueDataService>();

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

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            return app;
        }

        public static async Task SetupDatabase(WebApplication app)
        {

            Log.Logger.Information("Setup Database");

            //Create database if it does not exist
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
                using (var context = dbContextFactory.CreateDbContext())
                {
                    //if (app.Environment.IsDevelopment())
                    //{
                    Log.Logger.Information("Performing Migration");
                    await context.Database.MigrateAsync();
                    //}
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

        public static async Task SetupCaching(WebApplication app)
        {
            Log.Logger.Information("Setup Caching");
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var dbContextFactory = services.GetRequiredService<Microsoft.EntityFrameworkCore.IDbContextFactory<DataContext>>();
            using (var context = dbContextFactory.CreateDbContext())
            {
                var config = await context.Configurations.FindAsync(ConfigNames.IsCachingEnabled);
                if (config != null)
                {
                    if (config != null)
                    {
                        var cachingService = services.GetRequiredService<ICachingService>();
                        cachingService.IsCachingEnabled = true;
                        bool isCachingEnabled;

                        if (bool.TryParse(config.ConfigurationValue, out isCachingEnabled))
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

        public static void SetupEmailQueueProcessing(WebApplication app)
        {
            Log.Logger.Information("Setup Email Queue Processing");
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var configurationDataService = services.GetRequiredService<IConfigurationDataService>();
            var emailQueueDataService = services.GetRequiredService<IEmailQueueDataService>();
            EmailQueueLogic.Start(emailQueueDataService, configurationDataService);
        }
    }
}

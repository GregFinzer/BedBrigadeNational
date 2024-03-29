﻿using BedBrigade.Client.Components;
using BedBrigade.Client.Pages.Administration.Admin;
using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using BedBrigade.Data;
using BedBrigade.Data.Seeding;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor;
using Serilog;
using BedBrigade.Common;
using Microsoft.AspNetCore.Mvc;


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
            // Add services to the container.
            builder.Services.AddMvc(option => option.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            Log.Logger.Information("No Compression added to services");
            ////Do not use compression for hot reload of local development
            //if (!Common.Common.IsDevelopment())
            //{
            //    Log.Logger.Information("Configure Compression");
            //    builder.Services.Configure<GzipCompressionProviderOptions>
            //        (options => options.Level = CompressionLevel.Fastest);
            //    builder.Services.AddResponseCompression(options =>
            //    {
            //        options.Providers.Add<GzipCompressionProvider>();
            //    });
            //}

            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddBlazoredSessionStorage();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddDbContextFactory<DataContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlBuilder =>
                {
                    sqlBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseApplicationServiceProvider(_svcProvider);
            });
            
           
            builder.Services.AddHttpClient(); // VS 7/31/2023

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

            builder.Services.AddSyncfusionBlazor();
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MjIyMjE1NUAzMjMxMmUzMDJlMzBTR09FTlpnUWlNS1k5N0pualJ5UHdlYXNEVk1yakxlaTQrUmE0dEhBU1pJPQ==");

            _svcProvider = builder.Services.BuildServiceProvider();
        }


        private static void DataServices(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<ICommonService, CommonService>();
            builder.Services.AddScoped<ICustomSessionService, CustomSessionService>();
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

        private static void ClientServices(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ICachingService, CachingService>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ILoadImagesService, LoadImagesService>();
        }

        public static WebApplication CreateAndConfigureApplication(WebApplicationBuilder builder)
        {
            Log.Logger.Information("Create and configure application");
            WebApplication app = builder.Build();
            app.UsePathBase("/National");
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                Log.Logger.Information("No Compression added to the app");
                //Log.Logger.Information("Use Compression");
                //// Do not compress when using Hot Reload
                //app.UseResponseCompression();
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization(); // This must appear after the UseRouting middleware and before UseEndpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
                endpoints.MapControllers();
                endpoints.MapRazorPages();

            });
            
            Log.Information($"Connect Application Lifetime {app.Environment.ApplicationName}");
            
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

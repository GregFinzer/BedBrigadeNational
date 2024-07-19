using BedBrigade.Client.Services;
using BedBrigade.Common;
using BedBrigade.Data;
using BedBrigade.Data.Services;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);
            builder.Services.AddSyncfusionBlazor();

            builder.Services.AddBlazoredSessionStorage();
            ClientServices(builder);
            DataServices(builder);

            builder.Services.AddDbContextFactory<DataContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlBuilder =>
                {
                    sqlBuilder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseApplicationServiceProvider(_svcProvider);
            });

            
            

        }

        private static void ClientServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("ClientServices");
            builder.Services.AddSingleton<ICachingService, CachingService>();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ILoadImagesService, LoadImagesService>();
        }

        private static void DataServices(WebApplicationBuilder builder)
        {
            Log.Logger.Information("DataServices");
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
    }
}

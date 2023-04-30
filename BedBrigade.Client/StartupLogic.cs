using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using BedBrigade.Data;
using BedBrigade.Data.Seeding;
using BedBrigade.MessageService;
using Blazored.LocalStorage;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Blazor;
using Serilog;
using BedBrigade.MessageService.Services;
using Microsoft.EntityFrameworkCore.Internal;
using System.Data.Entity.Infrastructure;
using IHostingEnvironment = Microsoft.Extensions.Hosting.IHostingEnvironment;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Mvc;

namespace BedBrigade.Client
{
    public static class StartupLogic
    {

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

            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddBlazoredSessionStorage();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddDbContextFactory<DataContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            // Add Email Messageing Service config
            // Email Messaging Service
            EmailConfiguration emailConfig = builder.Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>();
            builder.Services.AddSingleton(emailConfig);
            //builder.Services.AddFluentEmail(emailConfig.From)
            //    .AddRazorRenderer()
            //    .AddSmtpSender(emailConfig.SmtpServer, emailConfig.Port, DeliveryMethod);

            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
            builder.Services.AddScoped<ILocationService, LocationService>();
            builder.Services.AddScoped<IContentService, ContentService>();
            builder.Services.AddScoped<IVolunteerService, VolunteerService>();
            builder.Services.AddScoped<IVolunteerForService, VolunteerForService>();
            builder.Services.AddScoped<IMediaService, MediaService>();
            builder.Services.AddScoped<IDonationService, DonationService>();
            builder.Services.AddScoped<IBedRequestService, BedRequestService>();

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

            builder.Services.AddScoped<IMessageService, Services.MessageService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

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
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("OTY1MjQ3QDMyMzAyZTM0MmUzMEJwUmQxRzhxTzFVRFN1RmFHc1VQdGZicmhyOVhVcW5MKy9NZXlPbEtETms9");
        }

        public static WebApplication CreateAndConfigureApplication(WebApplicationBuilder builder)
        {
            Log.Logger.Information("Create and configure application");
            WebApplication app = builder.Build();
            app.UsePathBase("/National");
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                // Do not compress when using Hot Reload
                app.UseResponseCompression();
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

            });
            Log.Information($"Connect Application Lifetime {app.Environment.ApplicationName}");
            // Connect the application lifetime
            IHostApplicationLifetime Lifetime = app.Lifetime;
            Lifetime.ApplicationStopping.Register(OnStopping);
            Lifetime.ApplicationStarted.Register(OnStarting);
            
            
            //Task.Run(async () => await  app.StopAsync());
            return app;
        }

        private static void OnStarting()
        {
            Log.Information($"Application Starting");
        }

        private static void OnStopping()
        {
            Log.Information($"Application Stopping");
            Log.CloseAndFlush();
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
                    if (app.Environment.IsDevelopment())
                    {
                        Log.Logger.Information("Performing Migration");
                        await context.Database.MigrateAsync();
                    }
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

    }
}

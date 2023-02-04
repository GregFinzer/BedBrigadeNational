﻿using BedBrigade.Server.Data;
using BedBrigade.Server.Data.Seeding;
using BedBrigade.Server.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

namespace BedBrigade.Server
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
            Log.Logger.Information("Add services to the container");
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ILocationService, LocationService>();
            builder.Services.AddScoped<IVolunteerService, VolunteerService>();
            builder.Services.AddScoped<IDonationService, DonationService>();
            builder.Services.AddScoped<IBedRequestService, BedRequestService>();

            builder.Services.AddControllers()
                .AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
            builder.Services.AddRazorPages();
            //services cors
            builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
            {
                builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
            }));

            //Configure Swagger
            builder.Services.AddSwaggerGen();
        }

        public static WebApplication CreateAndConfigureApplication(WebApplicationBuilder builder)
        {
            Log.Logger.Information("Create and configure application");

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                // Swagger should only be in developement
                //app.UseSwagger();
                //app.UseSwaggerUI();
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            //Explicitly only use blazor when the path doesn't start with api
            app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), blazor =>
            {
                blazor.UseStaticFiles();

                blazor.UseRouting();
                blazor.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToFile("index.html");
                });
            });

            //Explicitly map api endpoints only when path starts with api
            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), api =>
            {
                //if you are not using a blazor app, you can move these files out of this closure
                api.UseStaticFiles();
                api.UseRouting();
                api.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
            });
            app.UseCors("corsapp");
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
                var context = services.GetRequiredService<DataContext>();
                if (app.Environment.IsDevelopment())
                {
                    Log.Logger.Information("Performing Migration");
                    await context.Database.MigrateAsync();
                }

                Log.Logger.Information("Seeding Data");
                await Seed.SeedData(context);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "An error occurred during migration");
            }

        }

    }
}

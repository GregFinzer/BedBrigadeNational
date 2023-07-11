using BedBrigade.Client;
using BedBrigade.Client.Shared;
using Twilio.Rest.Api.V2010.Account.AvailablePhoneNumberCountry;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
StartupLogic.ConfigureLogger(builder);
StartupLogic.AddServicesToTheContainer(builder);
WebApplication app = StartupLogic.CreateAndConfigureApplication(builder);
await StartupLogic.SetupDatabase(app);
await StartupLogic.SetupCaching(app);

// testing seed Schedules- temporary changes
SeedSchedules.LoadTestSchedules(false); // set true to truncate table - remove all data

app.Run();


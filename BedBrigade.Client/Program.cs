using BedBrigade.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
StartupLogic.LoadConfiguration(builder);
StartupLogic.ConfigureLogger(builder);
StartupLogic.AddServicesToTheContainer(builder);
var app = StartupLogic.CreateAndConfigureApplication(builder);
await StartupLogic.SetupDatabase(app);
await StartupLogic.SetupCaching(app);
Log.Information("Startup Complete. Running...");
app.Run();

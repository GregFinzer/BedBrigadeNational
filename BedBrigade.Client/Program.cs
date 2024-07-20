using BedBrigade.Client;

var builder = WebApplication.CreateBuilder(args);
StartupLogic.ConfigureLogger(builder);
StartupLogic.AddServicesToTheContainer(builder);
var app = StartupLogic.CreateAndConfigureApplication(builder);
await StartupLogic.SetupDatabase(app);
await StartupLogic.SetupCaching(app);
StartupLogic.SetupEmailQueueProcessing(app);
app.Run();

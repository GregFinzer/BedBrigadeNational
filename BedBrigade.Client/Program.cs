using BedBrigade.Client;

var builder = WebApplication.CreateBuilder(args);
StartupLogic.LoadConfiguration(builder);
StartupLogic.ConfigureLogger(builder);
StartupLogic.AddServicesToTheContainer(builder);
var app = StartupLogic.CreateAndConfigureApplication(builder);
await StartupLogic.SetupDatabase(app);
await StartupLogic.SetupCaching(app);
StartupLogic.SetupSmsQueueProcessing(app);
app.Run();

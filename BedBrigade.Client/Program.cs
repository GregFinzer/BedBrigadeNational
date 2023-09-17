using BedBrigade.Client;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
StartupLogic.ConfigureLogger(builder);
StartupLogic.AddServicesToTheContainer(builder);
WebApplication app = StartupLogic.CreateAndConfigureApplication(builder);
await StartupLogic.SetupDatabase(app);
await StartupLogic.SetupCaching(app);
StartupLogic.SetupEmailQueueProcessing(app);
app.Run();


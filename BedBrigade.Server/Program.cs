using BedBrigade.Server;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
StartupLogic.ConfigureLogger(builder);
StartupLogic.AddServicesToTheContainer(builder);
WebApplication app = StartupLogic.CreateAndConfigureApplication(builder);
await StartupLogic.SetupDatabase(app);

app.Run();

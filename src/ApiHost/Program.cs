using AuditLogs.Api;

using AuditLogs.Infrastructure;

using ApiHost.Extensions;

using ApiHost.Middlewares;

using ApiHost.Startup;

using BackgroundJobs.Api;

using BackgroundJobs.Infrastructure;

using BuildingBlocks.Infrastructure.Health;

using BuildingBlocks.Web;

using Categories.Api;

using Categories.Infrastructure;

using Files.Api;

using Files.Infrastructure;

using Identity.Api;

using Identity.Infrastructure;

using Monitoring.Api;

using Monitoring.Infrastructure;

using Notifications.Api;

using Notifications.Infrastructure;

using Settings.Api;

using Settings.Infrastructure;
using Organizations.Infrastructure;
using Organizations.Api;
using AuthorizationPolicies.Infrastructure;
using AuthorizationPolicies.Api;

using Microsoft.Extensions.Hosting;

using Serilog;

using Users.Api;

using Users.Infrastructure;



try

{

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile(
        $"appsettings.{builder.Environment.EnvironmentName}.local.json",
        optional: true,
        reloadOnChange: true);

    ProductionStartupValidator.Validate(builder.Configuration, builder.Environment);



    var isIntegrationTesting = builder.Environment.IsEnvironment("IntegrationTesting");



    if (!isIntegrationTesting)

    {

        Log.Logger = new LoggerConfiguration()

            .WriteTo.Console()

            .CreateBootstrapLogger();



        builder.Host.UseSerilog((context, _, configuration) =>

            configuration

                .ReadFrom.Configuration(context.Configuration)

                .Enrich.FromLogContext());

    }



    builder.Services.AddApplicationServices(builder.Configuration);

    builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);

    builder.Services.AddBuildingBlocksWeb();

    builder.Services.AddApiSecurityServices(builder.Configuration, builder.Environment);



    builder.Services.AddAuditLogsInfrastructure(builder.Configuration);

    builder.Services.AddIdentityInfrastructure(builder.Configuration);

    builder.Services.AddIdentityApi();

    builder.Services.AddIdentityAuthentication(builder.Configuration, builder.Environment);



    builder.Services.AddUsersApi();

    builder.Services.AddUsersInfrastructure(builder.Configuration);

    builder.Services.AddCategoriesInfrastructure(builder.Configuration);

    builder.Services.AddCategoriesApi();

    builder.Services.AddSettingsInfrastructure(builder.Configuration);

    builder.Services.AddSettingsApi();

    builder.Services.AddOrganizationsInfrastructure(builder.Configuration);

    builder.Services.AddOrganizationsApi();

    builder.Services.AddAuthorizationPoliciesInfrastructure(builder.Configuration);

    builder.Services.AddAuthorizationPoliciesApi();

    builder.Services.AddFilesInfrastructure(builder.Configuration);

    builder.Services.AddFilesApi();

    builder.Services.AddNotificationsInfrastructure(builder.Configuration);

    builder.Services.AddNotificationsApi();

    builder.Services.AddBackgroundJobsInfrastructure(builder.Configuration);

    builder.Services.AddBackgroundJobsApi();

    builder.Services.AddMonitoringInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddMonitoringApi();

    builder.Services.AddAuditLogsApi();



    builder.Services.AddControllers()

        .AddStandardApiBehavior()

        .AddIdentityPresentation()

        .AddUsersPresentation()

        .AddCategoriesPresentation()

        .AddSettingsPresentation()

        .AddOrganizationsPresentation()

        .AddAuthorizationPoliciesPresentation()

        .AddFilesPresentation()

        .AddNotificationsPresentation()

        .AddBackgroundJobsPresentation()

        .AddMonitoringPresentation()

        .AddAuditLogsPresentation();



    builder.Services.AddAuthorization();

    builder.Services.AddSwaggerServices();



    var app = builder.Build();



    if (DatabaseMigrationStartup.ShouldApplyMigrations(app.Configuration))

    {

        await DatabaseMigrationStartup.ApplyMigrationsAndSeedAsync(app);

    }



    app.RegisterBackgroundJobsRecurringJobs();



    app.UseApiMiddlewares();

    app.UseApiSecurityMiddleware();



    if (ShouldExposeSwagger(app.Environment, app.Configuration))

    {

        app.UseSwagger();

        app.UseSwaggerUI();

    }



    if (!app.Environment.IsEnvironment("Docker"))

    {

        app.UseHttpsRedirection();

    }



    app.UseAuthentication();

    app.UseMiddleware<MaintenanceModeMiddleware>();

    app.UseAuthorization();



    app.UseBackgroundJobsDashboard();



    app.MapControllers();

    app.MapModularHealthChecks();



    app.Run();

}

catch (Exception ex)

{

    Log.Fatal(ex, "Application terminated unexpectedly.");

    throw;

}

finally

{

    if (!string.Equals(

            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),

            "IntegrationTesting",

            StringComparison.OrdinalIgnoreCase))

    {

        Log.CloseAndFlush();

    }

}



static bool ShouldExposeSwagger(IHostEnvironment environment, IConfiguration configuration)

{

    var enabled = configuration.GetValue("Swagger:Enabled", false);



    if (environment.IsProduction())

    {

        return false;

    }



    if (enabled)

    {

        return true;

    }



    return environment.IsDevelopment() || environment.IsEnvironment("Docker");

}



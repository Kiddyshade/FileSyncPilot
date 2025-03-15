
// This is the setup for a Windows Service application
using FilesyncPilot; // Main project components
using FilesyncPilot.Models; // Contains AppSettings configuration class
using FilesyncPilot.Services; // Contains our FileMonitorService
using Serilog;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection; // Tools for connecting services
using Microsoft.Extensions.Hosting; // Tools for building background services

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(configuration["Seq:Url"], apiKey: configuration["Seq:ApiKey"])
    .CreateLogger();

Log.Information("Hello from Seq! This is a test log message.");


// Create the application host (the core of our service)
IHost host = Host.CreateDefaultBuilder(args) // Start with default settings
    .UseSerilog()  // Use Serilog for logging
    .UseWindowsService() // Make it run as a Windows Service (like those in Services Manager)
    .ConfigureServices((hostContext, services) =>
    {
        // Load settings from config file (appsettings.json) into AppSettings class
        services.Configure<AppSettings>(
            hostContext.Configuration.GetSection("AppSettings")
        );

        // Add our FileMonitorService to run in the background
        services.AddHostedService<FileMonitorServices>();

        // Add health checks (you can add custom checks laters
        services.AddHealthChecks();
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
            });
        });
    })
    .Build(); // Finalize the setup

Log.Information("Starting Filesync Pilot...");

// Start the service and keep it running
await host.RunAsync(); // Like pressing "Start" on a machine

Log.CloseAndFlush();
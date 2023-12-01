using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Juice.BgService.Api;
using Juice.BgService.FileWatcher;
using Juice.BgService.Management;
using Juice.BgService.Tests;
using Juice.Extensions.Logging;
using Juice.Extensions.Options;
using Juice.Extensions.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var isService = !(Debugger.IsAttached || args.Contains("--console"));

WebApplicationOptions options = new()
{
    ContentRootPath = AppContext.BaseDirectory,
    Args = args.Where(arg => arg != "--console").ToArray()
};

var builder = WebApplication.CreateBuilder(options);

builder.Logging.AddFileLogger(builder.Configuration.GetSection("Logging:File"));
builder.Logging.AddSignalRLogger(builder.Configuration.GetSection("Logging:SignalR"));

// Add services to the container.

builder.Services.Configure<FileWatcherServiceOptions>(options => { options.MonitorPath = @"C:\Workspace\WatchFolder"; options.FileFilter = "."; });

builder.Services.AddBgService(builder.Configuration.GetSection("BackgroundService"))
    .UseFileStore(builder.Configuration.GetSection("File"))
    .SeparateStoreFile("Store", builder.Configuration, builder.Environment.EnvironmentName);

builder.Services.UseOptionsMutableFileStore("appsettings.Development.json");

builder.Services.AddControllers();

builder.Services.AddSwaggerWithDefaultConfigs()
    .ConfigureBgServiceSwaggerGen();

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddRazorPages();

var pluginPaths = new string[]
{
    GetPluginPath("Recurring", isService)
};

builder.Services.AddPlugins(options =>
{
    options.AbsolutePaths = pluginPaths;
    options.ConfigureSharedServices = (services, sp) =>
    {
    };
});

if (isService)
{
    builder.Host.UseWindowsService();
}
else
{
    builder.Host.UseConsoleLifetime();
}

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapRazorPages();
app.MapHub<LogHub>("/loghub");

app.UseBgServiceSwaggerUI();

app.Lifetime.ApplicationStopping.Register(async () =>
{
    Console.WriteLine($"Trying to stop services...");
    var service = app.Services.GetService<IServiceManager>();
    if (service != null)
    {
        try
        {
            await service.StopAsync(default);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to stop services. {ex.Message}");
        }
        Console.WriteLine($"Services stopped.");
    }
});

await app.RunAsync();


static string GetPluginPath(string pluginName, bool isService)
{

    return isService ? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\plugins", pluginName.ToLower(), $"Juice.BgService.Tests.{pluginName}.dll"))
        : Path.GetFullPath(Path.Combine("..\\..\\test", "plugins", pluginName.ToLower(), $"Juice.BgService.Tests.{pluginName}.dll"));
}

using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Application.Interfaces;
using dotnet_services_viewer.Infrastructure.SSH;
using dotnet_services_viewer.Infrastructure.Security;
using dotnet_services_viewer.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=monitoring.db"));

builder.Services.AddScoped<ISshClient, SshService>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddHostedService<MonitoringService>();

var app = builder.Build();

// ... (previous code)

app.UseAuthorization();

app.MapStaticAssets();

app.MapHub<dotnet_services_viewer.Hubs.MonitoringHub>("/monitoringHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

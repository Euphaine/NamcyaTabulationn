using NamcyaTabulation;
using NamcyaTabulation.Components;
using Microsoft.EntityFrameworkCore;
using NamcyaTabulation.Data;
using NamcyaTabulation.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.------------------------------------------------------------//
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();

// Ensure the database is stored in a writable user folder, not Program Files!
var dbFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NAMCYA_Tabulation");
Directory.CreateDirectory(dbFolder);
var dbPath = Path.Combine(dbFolder, "namcya.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Transient);

builder.Services.AddTransient<TabulationService>();
builder.Services.AddSingleton<ScoreBroadcastService>();
builder.Services.AddScoped<OrganizerAuthService>();
//------------------------------------------------------------------------------------------//
var app = builder.Build();

// Seed data for development purposes
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        // CRITICAL SAFETY: Only allow auto-deletion during local development.
        // In production, a temporary file lock could trigger this and wipe the live event data!
        if (app.Environment.IsDevelopment())
        {
            try 
            {
                context.Organizers.Any(); // Test if the table exists
            }
            catch
            {
                context.Database.EnsureDeleted(); // Nuke the corrupted file
            }
        }

        context.Database.EnsureCreated();

        if (!context.Organizers.Any())
        {
            var passwordHash = NamcyaTabulation.Services.OrganizerAuthService.HashPassword("admin123");
            context.Organizers.Add(new NamcyaTabulation.Models.Organizer { Username = "admin", PasswordHash = passwordHash });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine();
        Console.WriteLine($"DATABASE ERROR: {ex.Message}");
        if (!app.Environment.IsDevelopment()) Console.ReadLine(); // Keep the window open so we can read the error!
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<NamcyaTabulation.Components.App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://localhost:5000");
}
else 
{
    // Use * to allow ANY device on the local Wi-Fi to connect to the server!
    app.Urls.Add("http://*:5000");
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        if (!app.Environment.IsDevelopment())
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "http://localhost:5000",
                UseShellExecute = true
            });
        }
    }
    catch { }
});

try 
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"SERVER CRASHED: {ex.Message}");
    if (!app.Environment.IsDevelopment()) Console.ReadLine(); // Keep window open if it crashes!
}

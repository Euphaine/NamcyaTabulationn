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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)), ServiceLifetime.Transient);

builder.Services.AddTransient<TabulationService>();
builder.Services.AddSingleton<ScoreBroadcastService>();
builder.Services.AddScoped<OrganizerAuthService>();
//------------------------------------------------------------------------------------------//
var app = builder.Build();

// Seed data for development purposes
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();

    if (!context.Organizers.Any())
    {
        var passwordHash = NamcyaTabulation.Services.OrganizerAuthService.HashPassword("admin123");
        context.Organizers.Add(new NamcyaTabulation.Models.Organizer { Username = "admin", PasswordHash = passwordHash });
        context.SaveChanges();
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

app.Run();

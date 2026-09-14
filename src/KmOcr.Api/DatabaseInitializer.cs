using KmOcr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KmOcr.Api;

/// <summary>
/// Applies database migrations when the API starts.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Runs pending EF Core migrations or creates the starter schema when no migration exists yet.
    /// </summary>
    public static async Task ApplyMigrationsAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migrations = context.Database.GetMigrations();

        if (migrations.Any())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
        }
    }
}

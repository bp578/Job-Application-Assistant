using Microsoft.Data.SqlClient;

namespace JobAssistant.Api.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Creates the database and schema if they don't exist, retrying while SQL Server starts up.
    /// Phase 1 uses EnsureCreated for simplicity; see docs/decisions.md for the plan to move to migrations.
    /// </summary>
    public static async Task EnsureCreatedAsync(IServiceProvider services, ILogger logger, int maxAttempts = 10)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();
                return;
            }
            catch (SqlException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    "Database not ready (attempt {Attempt}/{MaxAttempts}): {Message}",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}

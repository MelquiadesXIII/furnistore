using System.Diagnostics;
using API.Furnistore.Application.Common;
using API.Furnistore.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Furnistore.API.Services
{
    public sealed class DatabaseWarmupService(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseWarmupService> logger
    ) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<APIFurnistoreContext>();

                await db.Products.AsNoTracking().Select(p => p.Id).FirstOrDefaultAsync(stoppingToken);

                logger.LogInformation(
                    ApiEvents.DatabaseWarmedUp,
                    "Modelo EF compilado y conexion abierta en {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ApiEvents.DatabaseWarmupFailed,
                    ex,
                    "El precalentamiento de la base fallo tras {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds
                );
            }
        }
    }
}

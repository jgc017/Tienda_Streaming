using Microsoft.EntityFrameworkCore;
using Tienda_Streaming.Data;

namespace Tienda_Streaming.Security
{
    public sealed class CuentasPasswordEncryptionHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CuentasPasswordEncryptionHostedService> _logger;

        public CuentasPasswordEncryptionHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<CuentasPasswordEncryptionHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var protector = scope.ServiceProvider.GetRequiredService<ICuentaPasswordProtector>();

                var cuentas = await context.Cuentas
                    .Where(c => c.Contrasena_Cuenta != string.Empty && !c.Contrasena_Cuenta.StartsWith("dp:"))
                    .ToListAsync(cancellationToken);

                if (cuentas.Count == 0)
                {
                    return;
                }

                foreach (var cuenta in cuentas)
                {
                    cuenta.Contrasena_Cuenta = protector.Protect(cuenta.Contrasena_Cuenta);
                    cuenta.Fecha_Modifica ??= DateTime.UtcNow;
                }

                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Se cifraron {Count} contrasenas de cuentas registradas.", cuentas.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No fue posible cifrar las contrasenas de cuentas legadas durante el arranque.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

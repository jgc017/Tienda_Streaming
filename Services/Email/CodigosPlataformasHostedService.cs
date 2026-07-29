using Microsoft.Extensions.Options;
using Tienda_Streaming.Business.Interfaces.CodigosPlataformas;

namespace Tienda_Streaming.Services.Email
{
    public class CodigosPlataformasHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptionsMonitor<CodigosPlataformasMailSettings> _settings;
        private readonly ILogger<CodigosPlataformasHostedService> _logger;
        private DateOnly? _ultimaLimpieza;

        public CodigosPlataformasHostedService(
            IServiceScopeFactory scopeFactory,
            IOptionsMonitor<CodigosPlataformasMailSettings> settings,
            ILogger<CodigosPlataformasHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var config = _settings.CurrentValue;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<ICodigosPlataformas>();

                    if (config.Enabled)
                    {
                        var importados = await service.SincronizarBuzon(stoppingToken);
                        if (importados > 0)
                        {
                            _logger.LogInformation("Correos de plataformas importados: {Cantidad}", importados);
                        }
                    }

                    await EjecutarLimpiezaSiCorresponde(service, config, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sincronizando bandeja de codigos de plataformas.");
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(config.PollIntervalSeconds, 30)), stoppingToken);
            }
        }

        private async Task EjecutarLimpiezaSiCorresponde(ICodigosPlataformas service, CodigosPlataformasMailSettings config, CancellationToken cancellationToken)
        {
            var ahora = ObtenerFechaHoraLocal();
            var fechaHoy = DateOnly.FromDateTime(ahora);
            if (_ultimaLimpieza == fechaHoy || ahora.Hour != config.CleanupHourLocal)
            {
                return;
            }

            var eliminados = await service.EliminarCorreosAntiguos(cancellationToken);
            _ultimaLimpieza = fechaHoy;

            if (eliminados > 0)
            {
                _logger.LogInformation("Correos de plataformas eliminados por retencion: {Cantidad}", eliminados);
            }
        }

        private static DateTime ObtenerFechaHoraLocal()
        {
            foreach (var zoneId in new[] { "America/Bogota", "SA Pacific Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zoneId));
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return DateTime.UtcNow;
        }
    }
}

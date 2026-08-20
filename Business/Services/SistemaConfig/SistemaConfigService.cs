using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.SistemaConfig;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.SistemaConfig;

namespace Tienda_Streaming.Business.Services.SistemaConfig
{
    // Servicio de negocio para la configuracion visual global del sistema.
    // Las imagenes permanecen en SistemaVisualConfig; el nombre se guarda en archivo para no crear registros en tablas.
    public class SistemaConfigService : ISistemaConfig
    {
        private const string LogoDefault = "/uploads/sistema/logo_20260801014229_436a9b3c1af244fca0be8dd75454e495.png";
        private const string FaviconDefault = "/favicon.ico";
        private const string LoginBackgroundDefault = "/uploads/sistema/loginbackground_20260801014351_1b013f02f27345b4ac09677148c3ffe3.png";
        private const string NombreSistemaDefault = "Tienda Streaming";
        private const string NombreConfigFile = "sistema-nombre.json";
        private readonly AppDbContext _context;
        private readonly ILogger<SistemaConfigService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public SistemaConfigService(
            AppDbContext context,
            ILogger<SistemaConfigService> logger,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        // F_GetSistemaVisualConfig: obtiene la configuracion vigente o retorna defaults.
        public async Task<DtoSistemaVisualConfigItem> F_GetSistemaVisualConfig()
        {
            try
            {
                var config = await _context.SistemaVisualConfig
                    .AsNoTracking()
                    .Where(c => c.Vigente == 1)
                    .OrderBy(c => c.Id_SistemaVisualConfig)
                    .Select(c => new DtoSistemaVisualConfigItem
                    {
                        Id_SistemaVisualConfig = c.Id_SistemaVisualConfig,
                        NombreSistema = c.NombreSistema,
                        LogoUrl = c.LogoUrl,
                        FaviconUrl = c.FaviconUrl,
                        LoginBackgroundUrl = c.LoginBackgroundUrl,
                        VideoUrl = c.VideoUrl
                    })
                    .FirstOrDefaultAsync();

                return config ?? ObtenerConfigDefault();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible leer SistemaVisualConfig. Se usaran imagenes por defecto.");
                var result = ObtenerConfigDefault();
                result.NombreSistema = ObtenerNombreSistema();
                return result;
            }
        }

        // P_UdpSistemaVisualConfig: crea o actualiza la configuracion visual global.
        public async Task<ServiceResult> P_UdpSistemaVisualConfig(DtoSistemaVisualConfigUpdateRequest model, AuditContext audit)
        {
            var nombreSistema = NormalizarNombreSistema(model.NombreSistema);
            var logo = NormalizarRuta(model.LogoUrl);
            var favicon = NormalizarRuta(model.FaviconUrl);
            var loginBackground = NormalizarRuta(model.LoginBackgroundUrl);
            var video = NormalizarVideoUrl(NormalizarRutaOpcional(model.VideoUrl));

            if (!NombreSistemaValido(nombreSistema))
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El nombre del sistema debe tener entre 2 y 120 caracteres.");
            }

            if (!RutaLocalValida(logo) || !RutaLocalValida(favicon) || !RutaLocalValida(loginBackground))
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Las rutas de imagen deben ser locales y comenzar por /uploads/ o ser /favicon.ico.");
            }

            if (!VideoValido(video))
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El video debe ser local en /video/ o una URL de YouTube valida.");
            }

            var config = await _context.SistemaVisualConfig
                .Where(c => c.Vigente == 1)
                .OrderBy(c => c.Id_SistemaVisualConfig)
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new SistemaVisualConfig
                {
                    NombreSistema = nombreSistema,
                    LogoUrl = logo,
                    FaviconUrl = favicon,
                    LoginBackgroundUrl = loginBackground,
                    VideoUrl = video,
                    Vigente = 1,
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                };

                _context.SistemaVisualConfig.Add(config);
            }
            else
            {
                config.NombreSistema = nombreSistema;
                config.LogoUrl = logo;
                config.FaviconUrl = favicon;
                config.LoginBackgroundUrl = loginBackground;
                config.VideoUrl = video;
                config.Id_Usuario_Modifica = audit.UserId;
                config.Fecha_Modifica = DateTime.UtcNow;
                config.Maquina_Modifica = audit.Machine;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Imagenes, videos y nombre del sistema actualizados correctamente.",
                await F_GetSistemaVisualConfig(),
                auditDescription: "Actualizacion de nombre, logo, favicon, fondo de login y video publico del sistema");
        }

        private DtoSistemaVisualConfigItem ObtenerConfigDefault()
        {
            var nombreConfigurado = NormalizarNombreSistema(_configuration["Sistema:Nombre"]);
            var nombreSistema = NombreSistemaValido(nombreConfigurado) ? nombreConfigurado : NombreSistemaDefault;

            return new DtoSistemaVisualConfigItem
            {
                NombreSistema = nombreSistema,
                LogoUrl = LogoDefault,
                FaviconUrl = FaviconDefault,
                LoginBackgroundUrl = LoginBackgroundDefault
            };
        }

        private static bool NombreSistemaValido(string? nombre)
        {
            return !string.IsNullOrWhiteSpace(nombre) && nombre.Length is >= 2 and <= 120;
        }

        private static string NormalizarNombreSistema(string? nombre)
        {
            return (nombre ?? string.Empty).Trim();
        }

        private static string NormalizarRuta(string ruta)
        {
            var value = ruta.Trim();
            return value.StartsWith("~/", StringComparison.Ordinal) ? value[1..] : value;
        }

        private static bool RutaLocalValida(string ruta)
        {
            return ruta.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ruta, "/favicon.ico", StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizarRutaOpcional(string? ruta)
        {
            var value = ruta?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.StartsWith("~/", StringComparison.Ordinal) ? value[1..] : value;
        }

        private static string? NormalizarVideoUrl(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return null;
            }

            if (ruta.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return ruta;
            }

            return TryGetYoutubeVideoId(ruta, out var videoId)
                ? $"https://www.youtube.com/embed/{videoId}?autoplay=1&mute=1&playsinline=1&rel=0"
                : ruta;
        }

        private static bool VideoValido(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta))
            {
                return true;
            }

            if (ruta.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Uri.TryCreate(ruta, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                && (uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Contains("youtube-nocookie.com", StringComparison.OrdinalIgnoreCase))
                && TryGetYoutubeVideoId(ruta, out _);
        }

        private static bool TryGetYoutubeVideoId(string url, out string videoId)
        {
            videoId = string.Empty;

            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            {
                return false;
            }

            var host = uri.Host.ToLowerInvariant();
            if (host.Contains("youtu.be"))
            {
                videoId = LimpiarYoutubeId(uri.AbsolutePath.Trim('/'));
                return !string.IsNullOrWhiteSpace(videoId);
            }

            if (!host.Contains("youtube.com") && !host.Contains("youtube-nocookie.com"))
            {
                return false;
            }

            var segmentos = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segmentos.Length >= 2
                && (segmentos[0].Equals("embed", StringComparison.OrdinalIgnoreCase)
                    || segmentos[0].Equals("shorts", StringComparison.OrdinalIgnoreCase)
                    || segmentos[0].Equals("live", StringComparison.OrdinalIgnoreCase)
                    || segmentos[0].Equals("v", StringComparison.OrdinalIgnoreCase)))
            {
                videoId = LimpiarYoutubeId(segmentos[1]);
                return !string.IsNullOrWhiteSpace(videoId);
            }

            if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
            {
                videoId = uri.Query.TrimStart('?')
                    .Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Split('=', 2))
                    .Where(p => p.Length == 2 && p[0].Equals("v", StringComparison.OrdinalIgnoreCase))
                    .Select(p => LimpiarYoutubeId(Uri.UnescapeDataString(p[1])))
                    .FirstOrDefault() ?? string.Empty;
            }

            return !string.IsNullOrWhiteSpace(videoId);
        }

        private static string LimpiarYoutubeId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var id = value.Split('/', '?', '&', '#').FirstOrDefault() ?? string.Empty;
            return new string(id.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        }
    }
}

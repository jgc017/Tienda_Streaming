using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.SistemaConfig;
using Tienda_Streaming.Models.Dto.Administracion.SistemaConfig;
using Tienda_Streaming.Security;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/SistemaConfig.js.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SistemaConfigApiController : ControllerBase
    {
        private static readonly HashSet<string> ExtensionesLogoPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        private static readonly HashSet<string> ExtensionesFaviconPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ico",
            ".png"
        };

        private static readonly HashSet<string> ExtensionesVideoPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4",
            ".webm",
            ".ogg"
        };

        private const long ImagenMaximaBytes = 5 * 1024 * 1024;
        private const long VideoMaximoBytes = 120 * 1024 * 1024;
        private readonly ISistemaConfig _sistemaConfig;
        private readonly IGeneral _general;
        private readonly IWebHostEnvironment _environment;

        public SistemaConfigApiController(ISistemaConfig sistemaConfig, IGeneral general, IWebHostEnvironment environment)
        {
            _sistemaConfig = sistemaConfig;
            _general = general;
            _environment = environment;
        }

        // GET: /api/SistemaConfigApi/F_GetSistemaVisualConfig
        [HttpGet("F_GetSistemaVisualConfig")]
        public async Task<IActionResult> F_GetSistemaVisualConfig()
        {
            if (!await TieneAccesoSistemaConfig())
            {
                return Forbid();
            }

            return Ok(new
            {
                ok = true,
                data = await _sistemaConfig.F_GetSistemaVisualConfig()
            });
        }

        // PUT: /api/SistemaConfigApi/P_UdpSistemaVisualConfig
        [HttpPut("P_UdpSistemaVisualConfig")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpSistemaVisualConfig([FromBody] DtoSistemaVisualConfigUpdateRequest model)
        {
            if (!await TieneAccesoSistemaConfig())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _sistemaConfig.P_UdpSistemaVisualConfig(model, GetAuditContext());
            if (result.Ok && !string.IsNullOrWhiteSpace(result.AuditDescription))
            {
                await _general.RegistrarAuditoria(GetAuditContext(), "VwSistemaConfig", "P_UdpSistemaVisualConfig", result.AuditDescription);
            }

            return StatusCode(result.StatusCode, result.ToApiResponse());
        }

        // POST: /api/SistemaConfigApi/P_UploadImagenSistema
        // Guarda una imagen en wwwroot/img/sistema y devuelve la ruta publica para el formulario.
        [HttpPost("P_UploadImagenSistema")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UploadImagenSistema(IFormFile imagen, [FromForm] string tipoImagen)
        {
            if (!await TieneAccesoSistemaConfig())
            {
                return Forbid();
            }

            if (imagen == null || imagen.Length == 0)
            {
                return BadRequest(new { ok = false, mensaje = "Debe seleccionar una imagen." });
            }

            if (imagen.Length > ImagenMaximaBytes)
            {
                return BadRequest(new { ok = false, mensaje = "La imagen no puede superar 5 MB." });
            }

            var tipo = (tipoImagen ?? string.Empty).Trim().ToLowerInvariant();
            if (tipo is not ("logo" or "favicon" or "loginbackground"))
            {
                return BadRequest(new { ok = false, mensaje = "Tipo de imagen no valido." });
            }

            var extension = Path.GetExtension(imagen.FileName);
            var extensionesPermitidas = tipo == "favicon" ? ExtensionesFaviconPermitidas : ExtensionesLogoPermitidas;
            if (!extensionesPermitidas.Contains(extension) || !await UploadSecurityValidator.FileMatchesExtension(imagen, extension))
            {
                var formatos = tipo == "favicon" ? "ICO o PNG" : "JPG, PNG, WEBP o GIF";
                return BadRequest(new { ok = false, mensaje = $"Formato no permitido. Usa {formatos}." });
            }

            var carpetaDestino = Path.Combine(_environment.WebRootPath, "img", "sistema");
            Directory.CreateDirectory(carpetaDestino);

            var nombreArchivo = $"{tipo}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

            await using (var stream = System.IO.File.Create(rutaFisica))
            {
                await imagen.CopyToAsync(stream);
            }

            var rutaPublica = $"/img/sistema/{nombreArchivo}";
            await _general.RegistrarAuditoria(
                GetAuditContext(),
                "VwSistemaConfig",
                "P_UploadImagenSistema",
                $"Carga de imagen visual {tipo}: {rutaPublica}");

            return Ok(new
            {
                ok = true,
                mensaje = "Imagen cargada correctamente.",
                data = rutaPublica
            });
        }

        // POST: /api/SistemaConfigApi/P_UploadVideoSistema
        // Reemplaza el video local de la configuracion visual y devuelve la ruta publica.
        [HttpPost("P_UploadVideoSistema")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UploadVideoSistema(IFormFile video, [FromForm] string? videoActual)
        {
            if (!await TieneAccesoSistemaConfig())
            {
                return Forbid();
            }

            if (video == null || video.Length == 0)
            {
                return BadRequest(new { ok = false, mensaje = "Debe seleccionar un video." });
            }

            if (video.Length > VideoMaximoBytes)
            {
                return BadRequest(new { ok = false, mensaje = "El video no puede superar 120 MB." });
            }

            var extension = Path.GetExtension(video.FileName);
            if (!ExtensionesVideoPermitidas.Contains(extension) || !await UploadSecurityValidator.FileMatchesExtension(video, extension))
            {
                return BadRequest(new { ok = false, mensaje = "Formato no permitido. Usa MP4, WEBM u OGG." });
            }

            var carpetaDestino = Path.Combine(_environment.WebRootPath, "video", "sistema");
            Directory.CreateDirectory(carpetaDestino);

            var nombreArchivo = $"sistema_video_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

            await using (var stream = System.IO.File.Create(rutaFisica))
            {
                await video.CopyToAsync(stream);
            }

            EliminarVideoLocalAnterior(videoActual);

            var rutaPublica = $"/video/sistema/{nombreArchivo}";
            await _general.RegistrarAuditoria(
                GetAuditContext(),
                "VwSistemaConfig",
                "P_UploadVideoSistema",
                $"Carga de video visual: {rutaPublica}");

            return Ok(new
            {
                ok = true,
                mensaje = "Video cargado correctamente.",
                data = rutaPublica
            });
        }

        private void EliminarVideoLocalAnterior(string? videoActual)
        {
            if (string.IsNullOrWhiteSpace(videoActual)
                || !videoActual.StartsWith("/video/sistema/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var nombreArchivo = Path.GetFileName(videoActual);
            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                return;
            }

            var rutaFisica = Path.Combine(_environment.WebRootPath, "video", "sistema", nombreArchivo);
            var raizVideos = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "video", "sistema"));
            var rutaNormalizada = Path.GetFullPath(rutaFisica);

            if (!rutaNormalizada.StartsWith(raizVideos, StringComparison.OrdinalIgnoreCase)
                || !System.IO.File.Exists(rutaNormalizada))
            {
                return;
            }

            System.IO.File.Delete(rutaNormalizada);
        }

        private BadRequestObjectResult InvalidModelStateResponse()
        {
            return BadRequest(new
            {
                ok = false,
                mensaje = "Datos invalidos",
                errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        private AuditContext GetAuditContext()
        {
            return new AuditContext(GetCurrentUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var usuarioId) ? usuarioId : null;
        }

        private List<int> GetCurrentUserRoles()
        {
            return User.FindAll("Id_Rol")
                .Select(c => int.TryParse(c.Value, out var idRol) ? idRol : 0)
                .Where(idRol => idRol > 0)
                .ToList();
        }

        private Task<bool> TieneAccesoSistemaConfig()
        {
            return _general.TienePermisoMenu(GetCurrentUserRoles(), "SistemaConfig", "VwSistemaConfig");
        }
    }
}

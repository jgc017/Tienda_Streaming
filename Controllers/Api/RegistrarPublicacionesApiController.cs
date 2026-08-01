using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.RegistrarPublicaciones;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarPublicaciones;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/RegistrarPublicaciones.js.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RegistrarPublicacionesApiController : ControllerBase
    {
        private static readonly HashSet<string> ExtensionesImagenPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".gif"
        };

        private const long ImagenMaximaBytes = 5 * 1024 * 1024;
        private const int DominioTipoContenidoInicio = 25;
        private const int TipoContenidoSlider = 26;
        private const int SliderAnchoMinimo = 1440;
        private const int SliderAltoMinimo = 600;
        private readonly IRegistrarPublicaciones _inicioAdmin;
        private readonly IGeneral _general;
        private readonly IWebHostEnvironment _environment;

        public RegistrarPublicacionesApiController(IRegistrarPublicaciones inicioAdmin, IGeneral general, IWebHostEnvironment environment)
        {
            _inicioAdmin = inicioAdmin;
            _general = general;
            _environment = environment;
        }

        // POST: /api/RegistrarPublicacionesApi/P_InsInicioContenido
        [HttpPost("P_InsInicioContenido")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsInicioContenido([FromBody] DtoInicioContenidoCreateRequest model)
        {
            if (!await TieneAccesoRegistrarPublicaciones())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _inicioAdmin.P_InsInicioContenido(model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarPublicaciones", "P_InsInicioContenido");
            return ApiResponse(result);
        }

        // GET: /api/RegistrarPublicacionesApi/F_GetInicioContenidosList
        // No audita porque alimenta la grilla administrativa.
        [HttpGet("F_GetInicioContenidosList")]
        public async Task<IActionResult> F_GetInicioContenidosList()
        {
            if (!await TieneAccesoRegistrarPublicaciones())
            {
                return Forbid();
            }

            return ApiResponse(await _inicioAdmin.F_GetInicioContenidosList());
        }

        // GET: /api/RegistrarPublicacionesApi/F_GetInicioContenido/{id}
        [HttpGet("F_GetInicioContenido/{id}")]
        public async Task<IActionResult> F_GetInicioContenido(int id)
        {
            if (!await TieneAccesoRegistrarPublicaciones())
            {
                return Forbid();
            }

            var result = await _inicioAdmin.F_GetInicioContenido(id);
            await AuditarOperacion(result, "VwRegistrarPublicaciones", "F_GetInicioContenido");
            return ApiResponse(result);
        }

        // PUT: /api/RegistrarPublicacionesApi/P_UdpInicioContenido/{id}
        [HttpPut("P_UdpInicioContenido/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpInicioContenido(int id, [FromBody] DtoInicioContenidoUpdateRequest model)
        {
            if (!await TieneAccesoRegistrarPublicaciones())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _inicioAdmin.P_UdpInicioContenido(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarPublicaciones", "P_UdpInicioContenido");
            return ApiResponse(result);
        }

        // DELETE: /api/RegistrarPublicacionesApi/P_DeleteInicioContenido/{id}
        [HttpDelete("P_DeleteInicioContenido/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteInicioContenido(int id)
        {
            if (!await TieneAccesoRegistrarPublicaciones())
            {
                return Forbid();
            }

            var result = await _inicioAdmin.P_DeleteInicioContenido(id, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarPublicaciones", "P_DeleteInicioContenido");
            return ApiResponse(result);
        }

        // POST: /api/RegistrarPublicacionesApi/P_UploadImagenInicio
        // Guarda una imagen en wwwroot/img/inicio y retorna la ruta publica.
        [HttpPost("P_UploadImagenInicio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UploadImagenInicio(IFormFile imagen, [FromForm] int idTipoContenido)
        {
            if (!await TieneAccesoRegistrarPublicaciones())
            {
                return Forbid();
            }

            if (imagen == null || imagen.Length == 0)
            {
                return BadRequest(new { ok = false, mensaje = "Debe seleccionar una imagen." });
            }

            if (!await TipoContenidoValido(idTipoContenido))
            {
                return BadRequest(new { ok = false, mensaje = "Debe seleccionar el tipo de contenido." });
            }

            if (imagen.Length > ImagenMaximaBytes)
            {
                return BadRequest(new { ok = false, mensaje = "La imagen no puede superar 5 MB." });
            }

            var extension = Path.GetExtension(imagen.FileName);
            if (!ExtensionesImagenPermitidas.Contains(extension))
            {
                return BadRequest(new { ok = false, mensaje = "Formato no permitido. Usa JPG, PNG, WEBP o GIF." });
            }

            var bytesImagen = await ObtenerBytesImagen(imagen);
            var dimensiones = ObtenerDimensionesImagen(bytesImagen, extension);
            if (dimensiones == null)
            {
                return BadRequest(new { ok = false, mensaje = "No fue posible leer las dimensiones de la imagen." });
            }

            if (EsSlider(idTipoContenido)
                && (dimensiones.Value.Width < SliderAnchoMinimo || dimensiones.Value.Height < SliderAltoMinimo))
            {
                return BadRequest(new
                {
                    ok = false,
                    mensaje = $"La imagen del slider debe medir minimo {SliderAnchoMinimo} x {SliderAltoMinimo} px. Imagen seleccionada: {dimensiones.Value.Width} x {dimensiones.Value.Height} px."
                });
            }

            var carpetaDestino = Path.Combine(_environment.WebRootPath, "img", "inicio");
            Directory.CreateDirectory(carpetaDestino);

            var nombreArchivo = $"inicio_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLower()}";
            var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

            await System.IO.File.WriteAllBytesAsync(rutaFisica, bytesImagen);

            var rutaPublica = $"/img/inicio/{nombreArchivo}";
            await _general.RegistrarAuditoria(
                GetAuditContext(),
                "VwRegistrarPublicaciones",
                "P_UploadImagenInicio",
                $"Carga de imagen de inicio {rutaPublica}");

            return Ok(new
            {
                ok = true,
                mensaje = "Imagen cargada correctamente.",
                data = rutaPublica,
                dimensiones = new { ancho = dimensiones.Value.Width, alto = dimensiones.Value.Height }
            });
        }

        private static bool EsSlider(int idTipoContenido)
        {
            return idTipoContenido == TipoContenidoSlider;
        }

        private async Task<bool> TipoContenidoValido(int idTipoContenido)
        {
            if (idTipoContenido <= 0)
            {
                return false;
            }

            var dominios = await _general.ObtenerDominiosPorPadre(DominioTipoContenidoInicio);
            return dominios.Any(d => d.Id_Dominio == idTipoContenido);
        }

        private static async Task<byte[]> ObtenerBytesImagen(IFormFile imagen)
        {
            using var memoryStream = new MemoryStream();
            await imagen.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        private static (int Width, int Height)? ObtenerDimensionesImagen(byte[] bytes, string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".png" => ObtenerDimensionesPng(bytes),
                ".jpg" => ObtenerDimensionesJpeg(bytes),
                ".jpeg" => ObtenerDimensionesJpeg(bytes),
                ".gif" => ObtenerDimensionesGif(bytes),
                ".webp" => ObtenerDimensionesWebp(bytes),
                _ => null
            };
        }

        private static (int Width, int Height)? ObtenerDimensionesPng(byte[] bytes)
        {
            if (bytes.Length < 24
                || bytes[0] != 0x89
                || bytes[1] != 0x50
                || bytes[2] != 0x4E
                || bytes[3] != 0x47)
            {
                return null;
            }

            return (LeerInt32BigEndian(bytes, 16), LeerInt32BigEndian(bytes, 20));
        }

        private static (int Width, int Height)? ObtenerDimensionesGif(byte[] bytes)
        {
            if (bytes.Length < 10
                || bytes[0] != 0x47
                || bytes[1] != 0x49
                || bytes[2] != 0x46)
            {
                return null;
            }

            return (LeerUInt16LittleEndian(bytes, 6), LeerUInt16LittleEndian(bytes, 8));
        }

        private static (int Width, int Height)? ObtenerDimensionesJpeg(byte[] bytes)
        {
            if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            {
                return null;
            }

            var posicion = 2;
            while (posicion + 9 < bytes.Length)
            {
                if (bytes[posicion] != 0xFF)
                {
                    posicion++;
                    continue;
                }

                while (posicion < bytes.Length && bytes[posicion] == 0xFF)
                {
                    posicion++;
                }

                if (posicion >= bytes.Length)
                {
                    return null;
                }

                var marcador = bytes[posicion++];
                if (marcador is 0xD8 or 0xD9 or 0x01)
                {
                    continue;
                }

                if (posicion + 1 >= bytes.Length)
                {
                    return null;
                }

                var longitud = LeerUInt16BigEndian(bytes, posicion);
                if (longitud < 2 || posicion + longitud > bytes.Length)
                {
                    return null;
                }

                if (EsMarcadorSofJpeg(marcador))
                {
                    if (posicion + 7 >= bytes.Length)
                    {
                        return null;
                    }

                    var alto = LeerUInt16BigEndian(bytes, posicion + 3);
                    var ancho = LeerUInt16BigEndian(bytes, posicion + 5);
                    return (ancho, alto);
                }

                posicion += longitud;
            }

            return null;
        }

        private static (int Width, int Height)? ObtenerDimensionesWebp(byte[] bytes)
        {
            if (bytes.Length < 30
                || bytes[0] != 0x52
                || bytes[1] != 0x49
                || bytes[2] != 0x46
                || bytes[3] != 0x46
                || bytes[8] != 0x57
                || bytes[9] != 0x45
                || bytes[10] != 0x42
                || bytes[11] != 0x50)
            {
                return null;
            }

            var chunk = System.Text.Encoding.ASCII.GetString(bytes, 12, 4);
            if (chunk == "VP8 " && bytes.Length >= 30)
            {
                return (LeerUInt16LittleEndian(bytes, 26) & 0x3FFF, LeerUInt16LittleEndian(bytes, 28) & 0x3FFF);
            }

            if (chunk == "VP8L" && bytes.Length >= 25)
            {
                var b0 = bytes[21];
                var b1 = bytes[22];
                var b2 = bytes[23];
                var b3 = bytes[24];
                var ancho = 1 + (((b1 & 0x3F) << 8) | b0);
                var alto = 1 + (((b3 & 0x0F) << 10) | (b2 << 2) | ((b1 & 0xC0) >> 6));
                return (ancho, alto);
            }

            if (chunk == "VP8X" && bytes.Length >= 30)
            {
                var ancho = 1 + LeerInt24LittleEndian(bytes, 24);
                var alto = 1 + LeerInt24LittleEndian(bytes, 27);
                return (ancho, alto);
            }

            return null;
        }

        private static bool EsMarcadorSofJpeg(byte marcador)
        {
            return marcador is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7 or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;
        }

        private static int LeerInt32BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
        }

        private static int LeerUInt16BigEndian(byte[] bytes, int offset)
        {
            return (bytes[offset] << 8) | bytes[offset + 1];
        }

        private static int LeerUInt16LittleEndian(byte[] bytes, int offset)
        {
            return bytes[offset] | (bytes[offset + 1] << 8);
        }

        private static int LeerInt24LittleEndian(byte[] bytes, int offset)
        {
            return bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16);
        }

        private IActionResult ApiResponse(ServiceResult result)
        {
            return StatusCode(result.StatusCode, result.ToApiResponse());
        }

        private async Task AuditarOperacion(ServiceResult result, string formulario, string metodoEjecutado)
        {
            if (result.Ok && !string.IsNullOrWhiteSpace(result.AuditDescription))
            {
                await _general.RegistrarAuditoria(GetAuditContext(), formulario, metodoEjecutado, result.AuditDescription);
            }
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

        private Task<bool> TieneAccesoRegistrarPublicaciones()
        {
            return _general.TienePermisoMenu(GetCurrentUserRoles(), "RegistrarPublicaciones", "VwRegistrarPublicaciones");
        }
    }
}



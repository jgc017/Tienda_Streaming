using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.ImagenesProducto;
using Tienda_Streaming.Models.Dto.Administracion.ImagenesProducto;
using Tienda_Streaming.Security;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImagenesProductoApiController : ControllerBase
    {
        private static readonly HashSet<string> ExtensionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private const long ImagenMaximaBytes = 5 * 1024 * 1024;
        private readonly IImagenesProducto _imagenesProducto;
        private readonly IGeneral _general;
        private readonly IWebHostEnvironment _environment;

        public ImagenesProductoApiController(IImagenesProducto imagenesProducto, IGeneral general, IWebHostEnvironment environment)
        {
            _imagenesProducto = imagenesProducto;
            _general = general;
            _environment = environment;
        }

        [HttpPost("P_InsImagenProducto")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsImagenProducto([FromBody] DtoImagenProductoCreateRequest model)
        {
            if (!await TieneAcceso()) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _imagenesProducto.P_InsImagenProducto(model, GetAuditContext());
            await AuditarOperacion(result, "VwImagenesProducto", "P_InsImagenProducto");
            return ApiResponse(result);
        }

        [HttpGet("F_GetImagenesProductoList")]
        public async Task<IActionResult> F_GetImagenesProductoList()
        {
            if (!await TieneAcceso()) return Forbid();
            return ApiResponse(await _imagenesProducto.F_GetImagenesProductoList());
        }

        [HttpGet("F_GetImagenProducto/{id}")]
        public async Task<IActionResult> F_GetImagenProducto(int id)
        {
            if (!await TieneAcceso()) return Forbid();
            var result = await _imagenesProducto.F_GetImagenProducto(id);
            await AuditarOperacion(result, "VwImagenesProducto", "F_GetImagenProducto");
            return ApiResponse(result);
        }

        [HttpPut("P_UdpImagenProducto/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpImagenProducto(int id, [FromBody] DtoImagenProductoUpdateRequest model)
        {
            if (!await TieneAcceso()) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _imagenesProducto.P_UdpImagenProducto(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwImagenesProducto", "P_UdpImagenProducto");
            return ApiResponse(result);
        }

        [HttpDelete("P_DeleteImagenProducto/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteImagenProducto(int id)
        {
            if (!await TieneAcceso()) return Forbid();
            var result = await _imagenesProducto.P_DeleteImagenProducto(id, GetAuditContext());
            await AuditarOperacion(result, "VwImagenesProducto", "P_DeleteImagenProducto");
            return ApiResponse(result);
        }

        [HttpPost("P_MoverImagenProducto")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_MoverImagenProducto([FromBody] DtoImagenProductoOrdenRequest model)
        {
            if (!await TieneAcceso()) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _imagenesProducto.P_MoverImagenProducto(model, GetAuditContext());
            await AuditarOperacion(result, "VwImagenesProducto", "P_MoverImagenProducto");
            return ApiResponse(result);
        }

        [HttpPost("P_UploadImagenProducto")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UploadImagenProducto(IFormFile imagen)
        {
            if (!await TieneAcceso()) return Forbid();

            if (imagen == null || imagen.Length == 0)
            {
                return BadRequest(new { ok = false, mensaje = "Debe seleccionar una imagen." });
            }

            if (imagen.Length > ImagenMaximaBytes)
            {
                return BadRequest(new { ok = false, mensaje = "La imagen no puede superar 5 MB." });
            }

            var extension = Path.GetExtension(imagen.FileName);
            if (!ExtensionesPermitidas.Contains(extension) || !await UploadSecurityValidator.FileMatchesExtension(imagen, extension))
            {
                return BadRequest(new { ok = false, mensaje = "Formato no permitido. Usa JPG, PNG o WEBP." });
            }

            var carpetaDestino = Path.Combine(_environment.WebRootPath, "img", "productos");
            Directory.CreateDirectory(carpetaDestino);

            var nombreArchivo = $"producto_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLower()}";
            var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

            await using (var stream = System.IO.File.Create(rutaFisica))
            {
                await imagen.CopyToAsync(stream);
            }

            var rutaPublica = $"/img/productos/{nombreArchivo}";
            await _general.RegistrarAuditoria(GetAuditContext(), "VwImagenesProducto", "P_UploadImagenProducto", $"Carga de imagen producto {rutaPublica}");

            return Ok(new { ok = true, mensaje = "Imagen cargada correctamente.", data = rutaPublica });
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
                errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
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

        private Task<bool> TieneAcceso()
        {
            return _general.TienePermisoMenu(GetCurrentUserRoles(), "ImagenesProducto", "VwImagenesProducto");
        }
    }
}

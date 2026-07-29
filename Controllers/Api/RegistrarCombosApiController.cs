using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarProductos;
using Tienda_Streaming.Security;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RegistrarCombosApiController : AdministracionApiControllerBase
    {
        private static readonly HashSet<string> ExtensionesImagenPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private const long ImagenMaximaBytes = 5 * 1024 * 1024;
        private readonly IRegistrarProductos _registrarProductos;
        private readonly IWebHostEnvironment _environment;

        public RegistrarCombosApiController(
            IRegistrarProductos registrarProductos,
            IGeneral general,
            IWebHostEnvironment environment) : base(general)
        {
            _registrarProductos = registrarProductos;
            _environment = environment;
        }

        [HttpPost("P_InsCombo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsCombo([FromBody] DtoComboRequest model)
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_InsCombo(model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarCombos", "P_InsCombo");
            return ApiResponse(result);
        }

        [HttpGet("F_GetCombosList")]
        public async Task<IActionResult> F_GetCombosList()
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetCombosList());
        }

        [HttpGet("F_GetCombo/{id}")]
        public async Task<IActionResult> F_GetCombo(int id)
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();
            var result = await _registrarProductos.F_GetCombo(id);
            await AuditarOperacion(result, "VwRegistrarCombos", "F_GetCombo");
            return ApiResponse(result);
        }

        [HttpPut("P_UdpCombo/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpCombo(int id, [FromBody] DtoComboRequest model)
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_UdpCombo(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarCombos", "P_UdpCombo");
            return ApiResponse(result);
        }

        [HttpDelete("P_DeleteCombo/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteCombo(int id)
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();
            var result = await _registrarProductos.P_DeleteCombo(id, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarCombos", "P_DeleteCombo");
            return ApiResponse(result);
        }

        [HttpPost("P_UploadImagenCombo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UploadImagenCombo(IFormFile imagen)
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();

            if (imagen == null || imagen.Length == 0)
            {
                return BadRequest(new { ok = false, mensaje = "Debe seleccionar una imagen." });
            }

            if (imagen.Length > ImagenMaximaBytes)
            {
                return BadRequest(new { ok = false, mensaje = "La imagen no puede superar 5 MB." });
            }

            var extension = Path.GetExtension(imagen.FileName);
            if (!ExtensionesImagenPermitidas.Contains(extension) || !await UploadSecurityValidator.FileMatchesExtension(imagen, extension))
            {
                return BadRequest(new { ok = false, mensaje = "Formato no permitido. Usa JPG, PNG o WEBP." });
            }

            var carpetaDestino = Path.Combine(_environment.WebRootPath, "img", "combos");
            Directory.CreateDirectory(carpetaDestino);

            var nombreArchivo = $"combo_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var rutaFisica = Path.Combine(carpetaDestino, nombreArchivo);

            await using (var stream = System.IO.File.Create(rutaFisica))
            {
                await imagen.CopyToAsync(stream);
            }

            var rutaPublica = $"/img/combos/{nombreArchivo}";
            await RegistrarAuditoria("VwRegistrarCombos", "P_UploadImagenCombo", $"Carga de imagen combo {rutaPublica}");

            return Ok(new { ok = true, mensaje = "Imagen cargada correctamente.", data = rutaPublica });
        }
    }
}

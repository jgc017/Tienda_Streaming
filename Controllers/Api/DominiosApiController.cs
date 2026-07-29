using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Dominios;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Dominios;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/Dominios.js.
    // El controlador valida la peticion HTTP y delega reglas de negocio a IDominios.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DominiosApiController : ControllerBase
    {
        private readonly IDominios _dominios;
        private readonly IGeneral _general;

        public DominiosApiController(IDominios dominios, IGeneral general)
        {
            _dominios = dominios;
            _general = general;
        }

        // POST: /api/DominiosApi/P_InsDominio
        [HttpPost("P_InsDominio")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsDominio([FromBody] DtoDominioCreateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _dominios.P_InsDominio(model, GetAuditContext());
            await AuditarOperacion(result, "VwDominios", "P_InsDominio");
            return ApiResponse(result);
        }

        // GET: /api/DominiosApi/F_GetDominiosList/{id_dominio}
        [HttpGet("F_GetDominiosList/{id_dominio}")]
        public async Task<IActionResult> F_GetDominiosList(int id_dominio)
        {
            return ApiResponse(await _dominios.F_GetDominiosList(id_dominio));
        }

        // GET: /api/DominiosApi/F_GetDominiosList
        [HttpGet("F_GetDominiosList")]
        public IActionResult F_GetDominiosList()
        {
            return ApiResponse(_dominios.F_GetDominiosList());
        }

        // GET: /api/DominiosApi/F_GetDominiosDropdown
        // Usa el servicio general para recargar el dropdown desde el flujo de dominios.
        [HttpGet("F_GetDominiosDropdown")]
        public async Task<IActionResult> F_GetDominiosDropdown()
        {
            var dominios = await _general.ObtenerDominios();
            return Ok(new { ok = true, data = dominios });
        }

        // GET: /api/DominiosApi/F_GetDominio/{id_dominio}
        [HttpGet("F_GetDominio/{id_dominio}")]
        public async Task<IActionResult> F_GetDominio(int id_dominio)
        {
            var result = await _dominios.F_GetDominio(id_dominio);
            await AuditarOperacion(result, "VwDominios", "F_GetDominio");
            return ApiResponse(result);
        }

        // PUT: /api/DominiosApi/P_UdpDominio/{id_dominio}
        [HttpPut("P_UdpDominio/{id_dominio}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpDominio(int id_dominio, [FromBody] DtoDominioUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _dominios.P_UdpDominio(id_dominio, model, GetAuditContext());
            await AuditarOperacion(result, "VwDominios", "P_UdpDominio");
            return ApiResponse(result);
        }

        // DELETE: /api/DominiosApi/P_DeleteDominio/{id}
        [HttpDelete("P_DeleteDominio/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteDominio(int id)
        {
            var result = await _dominios.P_DeleteDominio(id, GetAuditContext());
            await AuditarOperacion(result, "VwDominios", "P_DeleteDominio");
            return ApiResponse(result);
        }

        // Convierte el resultado de negocio en respuesta HTTP.
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

        // Respuesta comun para validaciones de atributos DataAnnotations.
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

        // Datos de auditoria obtenidos desde la cookie y la peticion actual.
        private AuditContext GetAuditContext()
        {
            return new AuditContext(GetCurrentUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var usuarioId) ? usuarioId : null;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.CodigosPlataformas;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CodigosPlataformasApiController : AdministracionApiControllerBase
    {
        private readonly ICodigosPlataformas _codigosPlataformas;

        public CodigosPlataformasApiController(ICodigosPlataformas codigosPlataformas, IGeneral general) : base(general)
        {
            _codigosPlataformas = codigosPlataformas;
        }

        [HttpGet("F_GetCorreosList")]
        public async Task<IActionResult> F_GetCorreosList()
        {
            if (!await TieneAccesoMenu("AdministracionCorreos", "VwCodigosPlataformas")) return Forbid();
            return ApiResponse(await _codigosPlataformas.F_GetCorreosAdminList());
        }

        [HttpGet("F_GetCorreoDetalle/{id}")]
        public async Task<IActionResult> F_GetCorreoDetalle(int id)
        {
            if (!await TieneAccesoMenu("AdministracionCorreos", "VwCodigosPlataformas")) return Forbid();

            var result = await _codigosPlataformas.F_GetCorreoAdminDetalle(id);
            await AuditarOperacion(result, "VwCodigosPlataformas", "F_GetCorreoDetalle");
            return ApiResponse(result);
        }

        [HttpPost("P_SincronizarBuzon")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_SincronizarBuzon()
        {
            if (!await TieneAccesoMenu("AdministracionCorreos", "VwCodigosPlataformas")) return Forbid();

            var cantidad = await _codigosPlataformas.SincronizarBuzon(HttpContext.RequestAborted);
            var result = ServiceResult.Success($"Sincronizacion completada. Correos importados: {cantidad}.", auditDescription: $"Sincronizacion manual codigos plataformas {cantidad}");
            await AuditarOperacion(result, "VwCodigosPlataformas", "P_SincronizarBuzon");
            return ApiResponse(result);
        }

        [HttpDelete("P_DeleteCorreo/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteCorreo(int id)
        {
            if (!await TieneAccesoMenu("AdministracionCorreos", "VwCodigosPlataformas")) return Forbid();

            var result = await _codigosPlataformas.P_DeleteCorreo(id, GetAuditContext());
            await AuditarOperacion(result, "VwCodigosPlataformas", "P_DeleteCorreo");
            return ApiResponse(result);
        }
    }
}

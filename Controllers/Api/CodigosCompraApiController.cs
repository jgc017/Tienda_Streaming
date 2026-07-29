using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarProductos;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CodigosCompraApiController : AdministracionApiControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public CodigosCompraApiController(IRegistrarProductos registrarProductos, IGeneral general) : base(general)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("P_GenerarCodigoCompra")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_GenerarCodigoCompra([FromBody] DtoCodigoCompraRequest model)
        {
            if (!await TieneAccesoMenu("CodigosCompra", "VwCodigosCompra")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_GenerarCodigoCompra(model, GetAuditContext());
            await AuditarOperacion(result, "VwCodigosCompra", "P_GenerarCodigoCompra");
            return ApiResponse(result);
        }

        [HttpGet("F_GetCodigosCompraList")]
        public async Task<IActionResult> F_GetCodigosCompraList()
        {
            if (!await TieneAccesoMenu("CodigosCompra", "VwCodigosCompra")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetCodigosCompraList());
        }

        [HttpGet("F_GetCodigoCompra/{id}")]
        public async Task<IActionResult> F_GetCodigoCompra(int id)
        {
            if (!await TieneAccesoMenu("CodigosCompra", "VwCodigosCompra")) return Forbid();
            var result = await _registrarProductos.F_GetCodigoCompra(id);
            await AuditarOperacion(result, "VwCodigosCompra", "F_GetCodigoCompra");
            return ApiResponse(result);
        }

        [HttpPut("P_UdpCodigoCompra/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpCodigoCompra(int id, [FromBody] DtoCodigoCompraUpdateRequest model)
        {
            if (!await TieneAccesoMenu("CodigosCompra", "VwCodigosCompra")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_UdpCodigoCompra(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwCodigosCompra", "P_UdpCodigoCompra");
            return ApiResponse(result);
        }

        [HttpDelete("P_DeleteCodigoCompra/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteCodigoCompra(int id)
        {
            if (!await TieneAccesoMenu("CodigosCompra", "VwCodigosCompra")) return Forbid();
            var result = await _registrarProductos.P_DeleteCodigoCompra(id, GetAuditContext());
            await AuditarOperacion(result, "VwCodigosCompra", "P_DeleteCodigoCompra");
            return ApiResponse(result);
        }
    }
}

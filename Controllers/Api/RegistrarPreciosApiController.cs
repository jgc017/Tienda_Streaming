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
    public class RegistrarPreciosApiController : AdministracionApiControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public RegistrarPreciosApiController(IRegistrarProductos registrarProductos, IGeneral general) : base(general)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("P_InsPrecioProducto")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsPrecioProducto([FromBody] DtoPrecioProductoRequest model)
        {
            if (!await TieneAccesoMenu("RegistrarPrecios", "VwRegistrarPrecios")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_InsPrecioProducto(model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarPrecios", "P_InsPrecioProducto");
            return ApiResponse(result);
        }

        [HttpGet("F_GetPreciosProductoList")]
        public async Task<IActionResult> F_GetPreciosProductoList()
        {
            if (!await TieneAccesoMenu("RegistrarPrecios", "VwRegistrarPrecios")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetPreciosProductoList());
        }

        [HttpPut("P_UdpPrecioProducto/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpPrecioProducto(int id, [FromBody] DtoPrecioProductoRequest model)
        {
            if (!await TieneAccesoMenu("RegistrarPrecios", "VwRegistrarPrecios")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_UdpPrecioProducto(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarPrecios", "P_UdpPrecioProducto");
            return ApiResponse(result);
        }

        [HttpDelete("P_DeletePrecioProducto/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeletePrecioProducto(int id)
        {
            if (!await TieneAccesoMenu("RegistrarPrecios", "VwRegistrarPrecios")) return Forbid();
            var result = await _registrarProductos.P_DeletePrecioProducto(id, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarPrecios", "P_DeletePrecioProducto");
            return ApiResponse(result);
        }
    }
}

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
    public class TiendaInternaApiController : AdministracionApiControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public TiendaInternaApiController(IRegistrarProductos registrarProductos, IGeneral general) : base(general)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("P_ConfirmarCompra")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_ConfirmarCompra([FromBody] DtoConfirmarCompraRequest model)
        {
            if (!await TieneAccesoTienda()) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_ConfirmarCompraInterna(model, GetAuditContext());
            await AuditarOperacion(result, "VwTiendas", "P_ConfirmarCompra");
            return ApiResponse(result);
        }

        [HttpGet("F_GetSaldoBilletera")]
        public async Task<IActionResult> F_GetSaldoBilletera()
        {
            if (!await TieneAccesoTienda()) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetSaldoBilletera(GetCurrentUserId() ?? 0));
        }
    }
}

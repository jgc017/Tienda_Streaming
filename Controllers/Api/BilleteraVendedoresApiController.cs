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
    public class BilleteraVendedoresApiController : AdministracionApiControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public BilleteraVendedoresApiController(IRegistrarProductos registrarProductos, IGeneral general) : base(general)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("P_RecargarBilletera")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_RecargarBilletera([FromBody] DtoRecargaBilleteraRequest model)
        {
            if (!await TieneAccesoMenu("BilleteraVendedores", "VwBilleteraVendedores")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_RecargarBilletera(model, GetAuditContext());
            await AuditarOperacion(result, "VwBilleteraVendedores", "P_RecargarBilletera");
            return ApiResponse(result);
        }

        [HttpGet("F_GetBilleterasList")]
        public async Task<IActionResult> F_GetBilleterasList()
        {
            if (!await TieneAccesoMenu("BilleteraVendedores", "VwBilleteraVendedores")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetBilleterasList());
        }

        [HttpGet("F_GetBilletera/{id}")]
        public async Task<IActionResult> F_GetBilletera(int id)
        {
            if (!await TieneAccesoMenu("BilleteraVendedores", "VwBilleteraVendedores")) return Forbid();
            var result = await _registrarProductos.F_GetBilletera(id);
            await AuditarOperacion(result, "VwBilleteraVendedores", "F_GetBilletera");
            return ApiResponse(result);
        }

        [HttpPut("P_UdpBilletera/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpBilletera(int id, [FromBody] DtoBilleteraUpdateRequest model)
        {
            if (!await TieneAccesoMenu("BilleteraVendedores", "VwBilleteraVendedores")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_UdpBilletera(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwBilleteraVendedores", "P_UdpBilletera");
            return ApiResponse(result);
        }
    }
}

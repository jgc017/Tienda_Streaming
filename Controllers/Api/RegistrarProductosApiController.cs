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
    public class RegistrarProductosApiController : AdministracionApiControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public RegistrarProductosApiController(IRegistrarProductos registrarProductos, IGeneral general) : base(general)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("P_InsCuenta")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsCuenta([FromBody] DtoCuentaCreateRequest model)
        {
            if (!await TieneAccesoMenu("RegistrarProductos", "VwRegistrarProductos")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_InsCuenta(model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarProductos", "P_InsCuenta");
            return ApiResponse(result);
        }

        [HttpGet("F_GetCuentasList")]
        public async Task<IActionResult> F_GetCuentasList()
        {
            if (!await TieneAccesoMenu("RegistrarProductos", "VwRegistrarProductos")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetCuentasList());
        }

        [HttpGet("F_GetCuenta/{id}")]
        public async Task<IActionResult> F_GetCuenta(int id)
        {
            if (!await TieneAccesoMenu("RegistrarProductos", "VwRegistrarProductos")) return Forbid();
            var result = await _registrarProductos.F_GetCuenta(id);
            await AuditarOperacion(result, "VwRegistrarProductos", "F_GetCuenta");
            return ApiResponse(result);
        }

        [HttpPut("P_UdpCuenta/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpCuenta(int id, [FromBody] DtoCuentaUpdateRequest model)
        {
            if (!await TieneAccesoMenu("RegistrarProductos", "VwRegistrarProductos")) return Forbid();
            if (!ModelState.IsValid) return InvalidModelStateResponse();

            var result = await _registrarProductos.P_UdpCuenta(id, model, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarProductos", "P_UdpCuenta");
            return ApiResponse(result);
        }

        [HttpDelete("P_DeleteCuenta/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteCuenta(int id)
        {
            if (!await TieneAccesoMenu("RegistrarProductos", "VwRegistrarProductos")) return Forbid();
            var result = await _registrarProductos.P_DeleteCuenta(id, GetAuditContext());
            await AuditarOperacion(result, "VwRegistrarProductos", "P_DeleteCuenta");
            return ApiResponse(result);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HistorialComprasApiController : AdministracionApiControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public HistorialComprasApiController(IRegistrarProductos registrarProductos, IGeneral general) : base(general)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpGet("F_GetHistorialCompras")]
        public async Task<IActionResult> F_GetHistorialCompras()
        {
            if (!await TieneAccesoMenu("HistorialCompras", "VwHistorialCompras")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetHistorialCompras(GetCurrentUserId(), GetCurrentUserRoles()));
        }

        [HttpGet("F_GetDetalleCompra/{id}")]
        public async Task<IActionResult> F_GetDetalleCompra(int id)
        {
            if (!await TieneAccesoMenu("HistorialCompras", "VwHistorialCompras")) return Forbid();
            return ApiResponse(await _registrarProductos.F_GetDetalleCompra(id, GetCurrentUserId(), GetCurrentUserRoles()));
        }
    }
}

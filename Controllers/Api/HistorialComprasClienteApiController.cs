using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarProductos;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HistorialComprasClienteApiController : ControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public HistorialComprasClienteApiController(IRegistrarProductos registrarProductos)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("F_GetHistorialComprasCliente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> F_GetHistorialComprasCliente([FromBody] DtoHistorialClienteRequest model)
        {
            if (!ModelState.IsValid) return InvalidModelStateResponse();
            return ApiResponse(await _registrarProductos.F_GetHistorialComprasCliente(model));
        }

        [HttpPost("F_GetDetalleCompraCliente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> F_GetDetalleCompraCliente([FromBody] DtoDetalleCompraRequest model)
        {
            if (!ModelState.IsValid) return InvalidModelStateResponse();
            return ApiResponse(await _registrarProductos.F_GetDetalleCompraCliente(model));
        }

        private IActionResult ApiResponse(ServiceResult result)
        {
            return StatusCode(result.StatusCode, result.ToApiResponse());
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
    }
}

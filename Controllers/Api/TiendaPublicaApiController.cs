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
    public class TiendaPublicaApiController : ControllerBase
    {
        private readonly IRegistrarProductos _registrarProductos;

        public TiendaPublicaApiController(IRegistrarProductos registrarProductos)
        {
            _registrarProductos = registrarProductos;
        }

        [HttpPost("P_ValidarCodigoCompra")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_ValidarCodigoCompra([FromBody] DtoValidarCodigoCompraRequest model)
        {
            if (!ModelState.IsValid) return InvalidModelStateResponse();
            return ApiResponse(await _registrarProductos.P_ValidarCodigoCompra(model));
        }

        [HttpPost("P_ConfirmarCompraPublica")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_ConfirmarCompraPublica([FromBody] DtoCompraPublicaRequest model)
        {
            if (!ModelState.IsValid) return InvalidModelStateResponse();
            return ApiResponse(await _registrarProductos.P_ConfirmarCompraPublica(model, new AuditContext(null, HttpContext.Connection.RemoteIpAddress?.ToString())));
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

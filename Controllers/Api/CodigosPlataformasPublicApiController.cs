using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.CodigosPlataformas;
using Tienda_Streaming.Models.Dto.Administracion.CodigosPlataformas;

namespace Tienda_Streaming.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class CodigosPlataformasPublicApiController : ControllerBase
    {
        private readonly ICodigosPlataformas _codigosPlataformas;

        public CodigosPlataformasPublicApiController(ICodigosPlataformas codigosPlataformas)
        {
            _codigosPlataformas = codigosPlataformas;
        }

        [HttpPost("F_BuscarCorreos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> F_BuscarCorreos([FromBody] DtoBuscarCorreoPlataformaRequest model)
        {
            if (!ModelState.IsValid) return InvalidModelStateResponse();
            return ApiResponse(await _codigosPlataformas.F_BuscarCorreosPublico(model));
        }

        [HttpPost("F_GetCorreoDetalle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> F_GetCorreoDetalle([FromBody] DtoDetalleCorreoPlataformaRequest model)
        {
            if (!ModelState.IsValid) return InvalidModelStateResponse();
            return ApiResponse(await _codigosPlataformas.F_GetCorreoPublicoDetalle(model));
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

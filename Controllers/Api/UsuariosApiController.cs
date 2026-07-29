using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.Usuarios;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Usuarios;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/Usuarios.js.
    // El controlador valida HTTP/sesion y delega reglas de negocio a IUsuarios.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosApiController : ControllerBase
    {
        private readonly IUsuarios _usuarios;
        private readonly IGeneral _general;

        public UsuariosApiController(IUsuarios usuarios, IGeneral general)
        {
            _usuarios = usuarios;
            _general = general;
        }

        // POST: /api/UsuariosApi/P_InsUsuario
        // Permite crear el primer usuario sin login; luego exige sesion activa.
        [HttpPost("P_InsUsuario")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> P_InsUsuario([FromBody] DtoUsuarioCreateRequest model)
        {
            var existenUsuarios = await _usuarios.ExistenUsuarios();
            if (existenUsuarios && User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { ok = false, mensaje = "Debes iniciar sesion para registrar usuarios." });
            }

            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var linkAcceso = Url.Action("Login", "Account", null, Request.Scheme);
            var result = await _usuarios.P_InsUsuario(model, GetAuditContext(), !existenUsuarios, linkAcceso);
            await AuditarOperacion(result, "VwUsuarios", "P_InsUsuario");
            return ApiResponse(result);
        }

        // GET: /api/UsuariosApi/F_GetUsuariosList
        // Permite listar durante el arranque inicial; luego exige sesion.
        [HttpGet("F_GetUsuariosList")]
        [AllowAnonymous]
        public async Task<IActionResult> F_GetUsuariosList()
        {
            var existenUsuarios = await _usuarios.ExistenUsuarios();
            if (existenUsuarios && User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new { ok = false, mensaje = "Debes iniciar sesion." });
            }

            return ApiResponse(await _usuarios.F_GetUsuariosList());
        }

        // GET: /api/UsuariosApi/F_GetUsuario/{id_Usuario}
        [HttpGet("F_GetUsuario/{id_Usuario}")]
        public async Task<IActionResult> F_GetUsuario(int id_Usuario)
        {
            var result = await _usuarios.F_GetUsuario(id_Usuario);
            await AuditarOperacion(result, "VwUsuarios", "F_GetUsuario");
            return ApiResponse(result);
        }

        // PUT: /api/UsuariosApi/P_UdpUsuario/{id_Usuario}
        [HttpPut("P_UdpUsuario/{id_Usuario}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpUsuario(int id_Usuario, [FromBody] DtoUsuarioUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _usuarios.P_UdpUsuario(id_Usuario, model, GetAuditContext());
            await AuditarOperacion(result, "VwUsuarios", "P_UdpUsuario");
            return ApiResponse(result);
        }

        // DELETE: /api/UsuariosApi/P_DeleteUsuario/{id}
        [HttpDelete("P_DeleteUsuario/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteUsuario(int id)
        {
            var result = await _usuarios.P_DeleteUsuario(id, GetAuditContext());
            await AuditarOperacion(result, "VwUsuarios", "P_DeleteUsuario");
            return ApiResponse(result);
        }

        private IActionResult ApiResponse(ServiceResult result)
        {
            return StatusCode(result.StatusCode, result.ToApiResponse());
        }

        private async Task AuditarOperacion(ServiceResult result, string formulario, string metodoEjecutado)
        {
            if (result.Ok && !string.IsNullOrWhiteSpace(result.AuditDescription))
            {
                await _general.RegistrarAuditoria(GetAuditContext(), formulario, metodoEjecutado, result.AuditDescription);
            }
        }

        private BadRequestObjectResult InvalidModelStateResponse()
        {
            return BadRequest(new
            {
                ok = false,
                mensaje = "Datos invalidos",
                errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            });
        }

        private AuditContext GetAuditContext()
        {
            return new AuditContext(GetCurrentUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var usuarioId) ? usuarioId : null;
        }
    }
}

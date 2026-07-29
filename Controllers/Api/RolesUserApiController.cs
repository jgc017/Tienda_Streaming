using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.RolesUser;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.RolesUser;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/Usuarios.js para asignar roles.
    // El controlador valida la peticion HTTP y delega sincronizacion a IRolesUser.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesUserApiController : ControllerBase
    {
        private readonly IRolesUser _rolesUser;
        private readonly IGeneral _general;

        public RolesUserApiController(IRolesUser rolesUser, IGeneral general)
        {
            _rolesUser = rolesUser;
            _general = general;
        }

        // GET: /api/RolesUserApi/GetIdUserRoles/{id_Usuario}
        [HttpGet("GetIdUserRoles/{id_Usuario}")]
        public async Task<IActionResult> GetIdUserRoles(int id_Usuario)
        {
            var result = await _rolesUser.GetIdUserRoles(id_Usuario);
            await AuditarOperacion(result, "VwUsuarios", "F_GetUsuarioRoles");
            return ApiResponse(result);
        }

        // PUT: /api/RolesUserApi/asignar/{id_Usuario}
        [HttpPut("asignar/{id_Usuario}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar(int id_Usuario, [FromBody] DtoRolesUserUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { ok = false, mensaje = "Datos invalidos." });
            }

            var result = await _rolesUser.Asignar(id_Usuario, model, GetAuditContext());
            await AuditarOperacion(result, "VwUsuarios", "P_UdpUsuarioRoles");
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.Roles;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Roles;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/Roles.js.
    // El controlador valida la peticion HTTP y delega reglas de negocio a IRoles.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RolesApiController : ControllerBase
    {
        private readonly IRoles _roles;
        private readonly IGeneral _general;

        public RolesApiController(IRoles roles, IGeneral general)
        {
            _roles = roles;
            _general = general;
        }

        // POST: /api/RolesApi/P_InsRol
        [HttpPost("P_InsRol")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsRol([FromBody] DtoRolCreateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _roles.P_InsRol(model, GetAuditContext());
            await AuditarOperacion(result, "Roles", "P_InsRol");
            return ApiResponse(result);
        }

        // GET: /api/RolesApi/F_GetRolesList
        [HttpGet("F_GetRolesList")]
        public async Task<IActionResult> F_GetRolesList()
        {
            return ApiResponse(await _roles.F_GetRolesList());
        }

        // GET: /api/RolesApi/F_GetRol/{id_Rol}
        [HttpGet("F_GetRol/{id_Rol}")]
        public async Task<IActionResult> F_GetRol(int id_Rol)
        {
            var result = await _roles.F_GetRol(id_Rol);
            await AuditarOperacion(result, "Roles", "F_GetRol");
            return ApiResponse(result);
        }

        // PUT: /api/RolesApi/P_UdpRol/{id_Rol}
        [HttpPut("P_UdpRol/{id_Rol}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpRol(int id_Rol, [FromBody] DtoRolUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _roles.P_UdpRol(id_Rol, model, GetAuditContext());
            await AuditarOperacion(result, "Roles", "P_UdpRol");
            return ApiResponse(result);
        }

        // DELETE: /api/RolesApi/P_DeleteRol/{id}
        [HttpDelete("P_DeleteRol/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeleteRol(int id)
        {
            var result = await _roles.P_DeleteRol(id, GetAuditContext());
            await AuditarOperacion(result, "Roles", "P_DeleteRol");
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

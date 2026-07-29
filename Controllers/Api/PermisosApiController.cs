using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.Permisos;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Permisos;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/Permisos.js.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermisosApiController : ControllerBase
    {
        private readonly IPermisos _permisos;
        private readonly IGeneral _general;

        public PermisosApiController(IPermisos permisos, IGeneral general)
        {
            _permisos = permisos;
            _general = general;
        }

        // POST: /api/PermisosApi/P_InsPermiso
        [HttpPost("P_InsPermiso")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsPermiso([FromBody] DtoPermisoCreateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _permisos.P_InsPermiso(model, GetAuditContext());
            await AuditarOperacion(result, "Permisos", "P_InsPermiso");
            return ApiResponse(result);
        }

        // GET: /api/PermisosApi/F_GetPermisosList
        // No audita porque es carga de tabla.
        [HttpGet("F_GetPermisosList")]
        public async Task<IActionResult> F_GetPermisosList()
        {
            return ApiResponse(await _permisos.F_GetPermisosList());
        }

        // GET: /api/PermisosApi/F_GetPermiso/{id_Permiso}
        [HttpGet("F_GetPermiso/{id_Permiso}")]
        public async Task<IActionResult> F_GetPermiso(int id_Permiso)
        {
            var result = await _permisos.F_GetPermiso(id_Permiso);
            await AuditarOperacion(result, "Permisos", "F_GetPermiso");
            return ApiResponse(result);
        }

        // PUT: /api/PermisosApi/P_UdpPermiso/{id_Permiso}
        [HttpPut("P_UdpPermiso/{id_Permiso}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpPermiso(int id_Permiso, [FromBody] DtoPermisoUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _permisos.P_UdpPermiso(id_Permiso, model, GetAuditContext());
            await AuditarOperacion(result, "Permisos", "P_UdpPermiso");
            return ApiResponse(result);
        }

        // DELETE: /api/PermisosApi/P_DeletePermiso/{id_Permiso}
        [HttpDelete("P_DeletePermiso/{id_Permiso}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeletePermiso(int id_Permiso)
        {
            var result = await _permisos.P_DeletePermiso(id_Permiso, GetAuditContext());
            await AuditarOperacion(result, "Permisos", "P_DeletePermiso");
            return ApiResponse(result);
        }

        // GET: /api/PermisosApi/F_GetRolesPorPermiso/{id_Permiso}
        // No audita porque alimenta la lista del modal de asignacion.
        [HttpGet("F_GetRolesPorPermiso/{id_Permiso}")]
        public async Task<IActionResult> F_GetRolesPorPermiso(int id_Permiso)
        {
            return ApiResponse(await _permisos.F_GetRolesPorPermiso(id_Permiso));
        }

        // PUT: /api/PermisosApi/P_UdpRolesPermiso/{id_Permiso}
        [HttpPut("P_UdpRolesPermiso/{id_Permiso}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpRolesPermiso(int id_Permiso, [FromBody] DtoPermisoRolBulkUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _permisos.P_UdpRolesPermiso(id_Permiso, model, GetAuditContext());
            await AuditarOperacion(result, "VwPermisos", "P_UdpRolesPermiso");
            return ApiResponse(result);
        }

        // POST: /api/PermisosApi/P_InsPermisoRol/{id_Permiso}
        [HttpPost("P_InsPermisoRol/{id_Permiso}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_InsPermisoRol(int id_Permiso, [FromBody] DtoPermisoRolCreateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _permisos.P_InsPermisoRol(id_Permiso, model, GetAuditContext());
            await AuditarOperacion(result, "VwPermisos", "P_InsPermisoRol");
            return ApiResponse(result);
        }

        // GET: /api/PermisosApi/F_GetPermisoRol/{id_Permiso}/{id_Rol}
        [HttpGet("F_GetPermisoRol/{id_Permiso}/{id_Rol}")]
        public async Task<IActionResult> F_GetPermisoRol(int id_Permiso, int id_Rol)
        {
            var result = await _permisos.F_GetPermisoRol(id_Permiso, id_Rol);
            await AuditarOperacion(result, "VwPermisos", "F_GetPermisoRol");
            return ApiResponse(result);
        }

        // DELETE: /api/PermisosApi/P_DeletePermisoRol/{id_Permiso}/{id_Rol}
        [HttpDelete("P_DeletePermisoRol/{id_Permiso}/{id_Rol}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeletePermisoRol(int id_Permiso, int id_Rol)
        {
            var result = await _permisos.P_DeletePermisoRol(id_Permiso, id_Rol, GetAuditContext());
            await AuditarOperacion(result, "VwPermisos", "P_DeletePermisoRol");
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

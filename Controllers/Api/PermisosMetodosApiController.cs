using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.Permisos;
using Tienda_Streaming.Models.Dto.Administracion.Permisos;

namespace Tienda_Streaming.Controllers.Api
{
    // API JSON usada por wwwroot/js/Administracion/PermisosMetodos.js.
    // Administra permisos automaticos generados desde metodos de controladores API.
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermisosMetodosApiController : AdministracionApiControllerBase
    {
        private readonly IPermisosMetodos _permisosMetodos;

        public PermisosMetodosApiController(IPermisosMetodos permisosMetodos, IGeneral general) : base(general)
        {
            _permisosMetodos = permisosMetodos;
        }

        // GET: /api/PermisosMetodosApi/F_GetPermisosMetodosList
        [HttpGet("F_GetPermisosMetodosList")]
        public async Task<IActionResult> F_GetPermisosMetodosList()
        {
            if (!await TieneAccesoMenu("Permisos", "VwPermisos")) return Forbid();
            return ApiResponse(await _permisosMetodos.F_GetPermisosMetodosList());
        }

        // GET: /api/PermisosMetodosApi/F_GetPermisoMetodo/{id_Permiso}
        [HttpGet("F_GetPermisoMetodo/{id_Permiso}")]
        public async Task<IActionResult> F_GetPermisoMetodo(int id_Permiso)
        {
            if (!await TieneAccesoMenu("Permisos", "VwPermisos")) return Forbid();
            var result = await _permisosMetodos.F_GetPermisoMetodo(id_Permiso);
            await AuditarOperacion(result, "VwPermisos", "F_GetPermisoMetodo");
            return ApiResponse(result);
        }

        // POST: /api/PermisosMetodosApi/P_SyncPermisosMetodos
        [HttpPost("P_SyncPermisosMetodos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_SyncPermisosMetodos()
        {
            if (!await TieneAccesoMenu("Permisos", "VwPermisos")) return Forbid();
            var result = await _permisosMetodos.P_SyncPermisosMetodos(GetAuditContext());
            await AuditarOperacion(result, "VwPermisos", "P_SyncPermisosMetodos");
            return ApiResponse(result);
        }

        // PUT: /api/PermisosMetodosApi/P_UdpPermisoMetodo/{id_Permiso}
        [HttpPut("P_UdpPermisoMetodo/{id_Permiso}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_UdpPermisoMetodo(int id_Permiso, [FromBody] DtoPermisoMetodoUpdateRequest model)
        {
            if (!await TieneAccesoMenu("Permisos", "VwPermisos")) return Forbid();
            if (!ModelState.IsValid)
            {
                return InvalidModelStateResponse();
            }

            var result = await _permisosMetodos.P_UdpPermisoMetodo(id_Permiso, model, GetAuditContext());
            await AuditarOperacion(result, "VwPermisos", "P_UdpPermisoMetodo");
            return ApiResponse(result);
        }

        // DELETE: /api/PermisosMetodosApi/P_DeletePermisoMetodo/{id_Permiso}
        [HttpDelete("P_DeletePermisoMetodo/{id_Permiso}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> P_DeletePermisoMetodo(int id_Permiso)
        {
            if (!await TieneAccesoMenu("Permisos", "VwPermisos")) return Forbid();
            var result = await _permisosMetodos.P_DeletePermisoMetodo(id_Permiso, GetAuditContext());
            await AuditarOperacion(result, "VwPermisos", "P_DeletePermisoMetodo");
            return ApiResponse(result);
        }
    }
}

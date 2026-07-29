using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Api
{
    public abstract class AdministracionApiControllerBase : ControllerBase
    {
        private readonly IGeneral _general;

        protected AdministracionApiControllerBase(IGeneral general)
        {
            _general = general;
        }

        protected IActionResult ApiResponse(ServiceResult result)
        {
            return StatusCode(result.StatusCode, result.ToApiResponse());
        }

        protected async Task AuditarOperacion(ServiceResult result, string formulario, string metodoEjecutado)
        {
            if (result.Ok && !string.IsNullOrWhiteSpace(result.AuditDescription))
            {
                await RegistrarAuditoria(formulario, metodoEjecutado, result.AuditDescription);
            }
        }

        protected Task RegistrarAuditoria(string formulario, string metodoEjecutado, string descripcion)
        {
            return _general.RegistrarAuditoria(GetAuditContext(), formulario, metodoEjecutado, descripcion);
        }

        protected BadRequestObjectResult InvalidModelStateResponse()
        {
            return BadRequest(new
            {
                ok = false,
                mensaje = "Datos invalidos",
                errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
            });
        }

        protected AuditContext GetAuditContext()
        {
            return new AuditContext(GetCurrentUserId(), HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        protected int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var usuarioId) ? usuarioId : null;
        }

        protected List<int> GetCurrentUserRoles()
        {
            return User.FindAll("Id_Rol")
                .Select(c => int.TryParse(c.Value, out var idRol) ? idRol : 0)
                .Where(idRol => idRol > 0)
                .ToList();
        }

        protected Task<bool> TieneAccesoMenu(string controlador, string vista)
        {
            return _general.TienePermisoMenu(GetCurrentUserRoles(), controlador, vista);
        }

        protected Task<bool> TieneAccesoTienda()
        {
            return _general.TienePermisoMenu(GetCurrentUserRoles(), "Tiendas", "VwTiendas");
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Administracion
{
    // Controlador MVC de la pantalla de configuracion visual del sistema.
    [Authorize]
    public class SistemaConfigController : Controller
    {
        private readonly IGeneral _general;

        public SistemaConfigController(IGeneral general)
        {
            _general = general;
        }

        // GET: /SistemaConfig/VwSistemaConfig
        // Renderiza el formulario para cambiar logo, favicon y fondo del login.
        public async Task<IActionResult> VwSistemaConfig()
        {
            if (!await _general.TienePermisoMenu(GetCurrentUserRoles(), "SistemaConfig", "VwSistemaConfig"))
            {
                return Forbid();
            }

            await _general.RegistrarAuditoria(GetAuditContext(), "VwSistemaConfig", "N/A", "Ingreso al formulario");
            return View();
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

        private List<int> GetCurrentUserRoles()
        {
            return User.FindAll("Id_Rol")
                .Select(c => int.TryParse(c.Value, out var idRol) ? idRol : 0)
                .Where(idRol => idRol > 0)
                .ToList();
        }
    }
}

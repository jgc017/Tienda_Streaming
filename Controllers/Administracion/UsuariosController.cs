using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Administracion
{
    // Controlador MVC de la pantalla de usuarios.
    // La seguridad fina del CRUD esta en UsuariosApiController; esta accion
    // solo entrega la vista Razor que consume esos endpoints por JavaScript.
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IGeneral _general;

        public UsuariosController(IGeneral general)
        {
            _general = general;
        }

        // GET: /Usuarios/VwUsuarios
        // Renderiza la pantalla de registro/listado/edicion de usuarios.
        public async Task<IActionResult> VwUsuarios()
        {
            await _general.RegistrarAuditoria(GetAuditContext(), "VwUsuarios", "N/A", "Ingreso al formulario");
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
    }
}

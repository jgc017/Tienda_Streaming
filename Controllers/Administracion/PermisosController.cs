using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Administracion
{
    // Controlador MVC de la pantalla de permisos.
    [Authorize]
    public class PermisosController : Controller
    {
        private readonly IGeneral _general;
        private const int DominioPermisos = 3;
        private const int AccionVer = 4;

        public PermisosController(IGeneral general)
        {
            _general = general;
        }

        // GET: /Permisos/VwPermisos
        // Renderiza la pantalla de CRUD de permisos de menu.
        public async Task<IActionResult> VwPermisos()
        {
            ViewBag.ddlMenus = new SelectList(await _general.ObtenerMenus(), "Id_Menu", "Descripcion");
            ViewBag.ddlAcciones = new SelectList(await _general.ObtenerDominiosPorPadre(DominioPermisos, AccionVer), "Descripcion", "Descripcion");

            await _general.RegistrarAuditoria(GetAuditContext(), "VwPermisos", "N/A", "Ingreso al formulario");
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

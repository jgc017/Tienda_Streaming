using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Tienda_Streaming.Controllers.Administracion
{
    // Controlador MVC de la pantalla de dominios.
    [Authorize]
    public class DominiosController : Controller
    {
        private readonly IGeneral _general;

        public DominiosController(IGeneral general)
        {
            _general = general;
        }

        // GET: /Dominios/VwDominios
        // Carga el dropdown de dominios padre para consultar y administrar hijos.
        public async Task<IActionResult> VwDominios()
        {
            ViewBag.ddlDominio = new SelectList(await _general.ObtenerDominios(), "Id_Dominio", "Descripcion");
            await _general.RegistrarAuditoria(GetAuditContext(), "VwDominios", "N/A", "Ingreso al formulario");
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class ImagenesProductoController : Controller
    {
        private const int DominioPlataformas = 10;
        private const int DominioTipoImagen = 34;
        private readonly IGeneral _general;

        public ImagenesProductoController(IGeneral general)
        {
            _general = general;
        }

        public async Task<IActionResult> VwImagenesProducto()
        {
            if (!await _general.TienePermisoMenu(GetCurrentUserRoles(), "ImagenesProducto", "VwImagenesProducto"))
            {
                return Forbid();
            }

            ViewBag.ddlPlataformas = new SelectList(await _general.ObtenerDominiosPorPadre(DominioPlataformas), "Id_Dominio", "Descripcion");
            ViewBag.ddlTiposImagen = new SelectList(await _general.ObtenerDominiosPorPadre(DominioTipoImagen), "Id_Dominio", "Descripcion");
            await _general.RegistrarAuditoria(GetAuditContext(), "VwImagenesProducto", "N/A", "Ingreso al formulario");
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

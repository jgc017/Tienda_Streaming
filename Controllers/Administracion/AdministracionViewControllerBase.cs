using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers.Administracion
{
    public abstract class AdministracionViewControllerBase : Controller
    {
        protected const int DominioPlataformas = 10;
        protected const int DominioTipoUsuario = 22;
        protected readonly IGeneral General;

        protected AdministracionViewControllerBase(IGeneral general)
        {
            General = general;
        }

        protected Task<bool> TieneAccesoMenu(string controlador, string vista)
        {
            return General.TienePermisoMenu(GetCurrentUserRoles(), controlador, vista);
        }

        protected async Task CargarCatalogosProductos()
        {
            ViewBag.ddlPlataformas = new SelectList(await General.ObtenerDominiosPorPadre(DominioPlataformas), "Id_Dominio", "Descripcion");
            ViewBag.ddlTiposUsuario = new SelectList(await General.ObtenerDominiosPorPadre(DominioTipoUsuario), "Id_Dominio", "Descripcion");
        }

        protected async Task CargarUsuarios()
        {
            ViewBag.ddlUsuarios = new SelectList(await General.ObtenerUsuarios(), "Id_Usuario", "Nombre");
        }

        protected Task RegistrarIngreso(string formulario)
        {
            return General.RegistrarAuditoria(GetAuditContext(), formulario, "N/A", "Ingreso al formulario");
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
    }
}

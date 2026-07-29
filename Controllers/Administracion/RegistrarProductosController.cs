using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class RegistrarProductosController : AdministracionViewControllerBase
    {
        public RegistrarProductosController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwRegistrarProductos()
        {
            if (!await TieneAccesoMenu("RegistrarProductos", "VwRegistrarProductos")) return Forbid();

            await CargarCatalogosProductos();
            await RegistrarIngreso("VwRegistrarProductos");
            return View();
        }
    }
}

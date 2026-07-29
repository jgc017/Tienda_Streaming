using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class RegistrarPreciosController : AdministracionViewControllerBase
    {
        public RegistrarPreciosController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwRegistrarPrecios()
        {
            if (!await TieneAccesoMenu("RegistrarPrecios", "VwRegistrarPrecios")) return Forbid();

            await CargarCatalogosProductos();
            await RegistrarIngreso("VwRegistrarPrecios");
            return View();
        }
    }
}

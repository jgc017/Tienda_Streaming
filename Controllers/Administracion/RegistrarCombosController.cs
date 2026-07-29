using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class RegistrarCombosController : AdministracionViewControllerBase
    {
        public RegistrarCombosController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwRegistrarCombos()
        {
            if (!await TieneAccesoMenu("RegistrarCombos", "VwRegistrarCombos")) return Forbid();

            await CargarCatalogosProductos();
            await RegistrarIngreso("VwRegistrarCombos");
            return View();
        }
    }
}

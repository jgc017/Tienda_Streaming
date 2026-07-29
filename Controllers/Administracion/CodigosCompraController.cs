using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class CodigosCompraController : AdministracionViewControllerBase
    {
        public CodigosCompraController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwCodigosCompra()
        {
            if (!await TieneAccesoMenu("CodigosCompra", "VwCodigosCompra")) return Forbid();

            await RegistrarIngreso("VwCodigosCompra");
            return View();
        }
    }
}

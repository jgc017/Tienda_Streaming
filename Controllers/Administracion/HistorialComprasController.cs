using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class HistorialComprasController : AdministracionViewControllerBase
    {
        public HistorialComprasController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwHistorialCompras()
        {
            if (!await TieneAccesoMenu("HistorialCompras", "VwHistorialCompras")) return Forbid();

            await RegistrarIngreso("VwHistorialCompras");
            return View();
        }
    }
}

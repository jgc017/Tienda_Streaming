using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class BilleteraVendedoresController : AdministracionViewControllerBase
    {
        public BilleteraVendedoresController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwBilleteraVendedores()
        {
            if (!await TieneAccesoMenu("BilleteraVendedores", "VwBilleteraVendedores")) return Forbid();

            await CargarUsuarios();
            await RegistrarIngreso("VwBilleteraVendedores");
            return View();
        }
    }
}

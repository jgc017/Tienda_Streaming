using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.General;

namespace Tienda_Streaming.Controllers.Administracion
{
    [Authorize]
    public class AdministracionCorreosController : AdministracionViewControllerBase
    {
        public AdministracionCorreosController(IGeneral general) : base(general)
        {
        }

        public async Task<IActionResult> VwCodigosPlataformas()
        {
            if (!await TieneAccesoMenu("AdministracionCorreos", "VwCodigosPlataformas")) return Forbid();

            await RegistrarIngreso("VwCodigosPlataformas");
            return View();
        }
    }
}

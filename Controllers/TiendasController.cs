using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Business.Interfaces.RegistrarPublicaciones;
using Tienda_Streaming.Business.Interfaces.SistemaConfig;
using Tienda_Streaming.Controllers;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarPublicaciones;
using System.Security.Claims;

namespace Tienda_Streaming.Controllers
{
    // Pantalla interna de tiendas para usuarios autenticados.
    // Reutiliza la tienda publica, pero con navegacion controlada desde Menus.
    [Authorize]
    public class TiendasController : Controller
    {
        private readonly IRegistrarPublicaciones _inicioAdmin;
        private readonly IRegistrarProductos _registrarProductos;
        private readonly ISistemaConfig _sistemaConfig;
        private readonly IGeneral _general;
        private const int TipoUsuarioVendedor = 24;
        private const int TipoImagenPantallaIndividual = 35;

        public TiendasController(IRegistrarPublicaciones inicioAdmin, IRegistrarProductos registrarProductos, ISistemaConfig sistemaConfig, IGeneral general)
        {
            _inicioAdmin = inicioAdmin;
            _registrarProductos = registrarProductos;
            _sistemaConfig = sistemaConfig;
            _general = general;
        }

        // GET: /Tiendas/VwTiendas
        public async Task<IActionResult> VwTiendas()
        {
            if (!await TieneAccesoTiendas())
            {
                return Forbid();
            }

            await CargarVideoPublico();
            await CargarProductosVendedor();
            return View("~/Views/Home/VwIndex.cshtml", await _inicioAdmin.F_GetInicioPublico());
        }

        // GET: /Tiendas/VwCombos
        public async Task<IActionResult> VwCombos()
        {
            return RedirectToAction(nameof(VwTiendas));
        }

        // GET: /Tiendas/VwContacto
        public async Task<IActionResult> VwContacto()
        {
            if (!await TieneAccesoTiendas())
            {
                return Forbid();
            }

            var data = await _inicioAdmin.F_GetContenidoPublicoPorTipo(DtoInicioContenidoTipos.Contacto);
            return View("~/Views/Home/VwContacto.cshtml", data);
        }

        private async Task CargarVideoPublico()
        {
            var config = await _sistemaConfig.F_GetSistemaVisualConfig();
            ViewBag.VideoUrl = config.VideoUrl;
        }

        private async Task CargarProductosVendedor()
        {
            ViewBag.IdTipoUsuarioTienda = TipoUsuarioVendedor;
            ViewBag.Productos = await _registrarProductos.F_GetProductosTienda(TipoUsuarioVendedor, TipoImagenPantallaIndividual);
        }

        private Task<bool> TieneAccesoTiendas()
        {
            return _general.TienePermisoMenu(GetCurrentUserRoles(), "Tiendas", "VwTiendas");
        }

        private List<int> GetCurrentUserRoles()
        {
            return User.FindAll(AccountController.RoleIdClaimType)
                .Select(c => int.TryParse(c.Value, out var idRol) ? idRol : 0)
                .Where(idRol => idRol > 0)
                .ToList();
        }
    }
}


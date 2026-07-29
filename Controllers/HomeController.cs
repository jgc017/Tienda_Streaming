using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Business.Interfaces.RegistrarPublicaciones;
using Tienda_Streaming.Business.Interfaces.SistemaConfig;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarPublicaciones;
using Tienda_Streaming.Models.Dto.General;
using System.Diagnostics;

namespace Tienda_Streaming.Controllers
{
    // Controlador de tienda publica y paginas generales del proyecto.
    public class HomeController : Controller
    {
        private readonly IRegistrarPublicaciones _inicioAdmin;
        private readonly IRegistrarProductos _registrarProductos;
        private readonly ISistemaConfig _sistemaConfig;
        private const int TipoUsuarioCliente = 23;
        private const int TipoImagenPantallaIndividual = 35;
        private const int TipoImagenCombo = 36;

        public HomeController(IRegistrarPublicaciones inicioAdmin, IRegistrarProductos registrarProductos, ISistemaConfig sistemaConfig)
        {
            _inicioAdmin = inicioAdmin;
            _registrarProductos = registrarProductos;
            _sistemaConfig = sistemaConfig;
        }

        // GET: /Home/VwIndex
        // Pagina publica Pantallas. No se muestra a usuarios autenticados.
        [AllowAnonymous]
        public async Task<IActionResult> VwIndex()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("VwTiendas", "Tiendas");
            }

            await CargarVideoPublico();
            await CargarProductosCliente(TipoImagenPantallaIndividual);
            return View(await _inicioAdmin.F_GetInicioPublico());
        }

        // GET: /Home/VwAcercaNosotros
        [AllowAnonymous]
        public IActionResult VwAcercaNosotros()
        {
            return NotFound();
        }

        // GET: /Home/VwNoticias
        [AllowAnonymous]
        public IActionResult VwNoticias()
        {
            return NotFound();
        }

        // GET: /Home/VwPublicaciones. Ruta antigua; ahora la seccion se llama Combos.
        [AllowAnonymous]
        public IActionResult VwPublicaciones()
        {
            return RedirectToAction(nameof(VwCombos));
        }

        // GET: /Home/VwCombos
        [AllowAnonymous]
        public async Task<IActionResult> VwCombos()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("VwTiendas", "Tiendas");
            }

            var data = new List<DtoInicioContenidoItem>();
            ViewBag.Contacto = (await _inicioAdmin.F_GetContenidoPublicoPorTipo(DtoInicioContenidoTipos.Contacto)).FirstOrDefault();
            await CargarVideoPublico();
            ViewBag.IdTipoUsuarioTienda = TipoUsuarioCliente;
            ViewBag.Combos = await _registrarProductos.F_GetCombosTienda(TipoUsuarioCliente);
            ViewData["Title"] = "Combos";
            return View("VwCombos", data);
        }

        // GET: /Home/VwContenidoDetalle/{id}
        [AllowAnonymous]
        public async Task<IActionResult> VwContenidoDetalle(int id)
        {
            var data = await _inicioAdmin.F_GetContenidoPublicoDetalle(id);
            if (data == null)
            {
                return NotFound();
            }

            return View(data);
        }

        // GET: /Home/VwContacto
        [AllowAnonymous]
        public async Task<IActionResult> VwContacto()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("VwContacto", "Tiendas");
            }

            var data = await _inicioAdmin.F_GetContenidoPublicoPorTipo(DtoInicioContenidoTipos.Contacto);
            return View(data);
        }

        // GET: /Home/VwHistorialComprasCliente
        [AllowAnonymous]
        public IActionResult VwHistorialComprasCliente()
        {
            ViewData["Title"] = "Historial";
            return View();
        }

        // GET: /Home/VwCodigosPlataformas
        [AllowAnonymous]
        public IActionResult VwCodigosPlataformas()
        {
            ViewData["Title"] = "Codigos Plataformas";
            return View();
        }

        // GET: /Home/VwPrivacy
        // Vista publica de politicas de privacidad.
        [AllowAnonymous]
        public IActionResult VwPrivacy()
        {
            return NotFound();
        }

        // GET: /Home/Error
        // Vista publica usada por el middleware de excepciones para mostrar
        // errores controlados sin exponer detalles internos.
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View("VwError", new DtoErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task CargarVideoPublico()
        {
            var config = await _sistemaConfig.F_GetSistemaVisualConfig();
            ViewBag.VideoUrl = config.VideoUrl;
        }

        private async Task CargarProductosCliente(int idTipoImagen)
        {
            ViewBag.IdTipoUsuarioTienda = TipoUsuarioCliente;
            ViewBag.Productos = await _registrarProductos.F_GetProductosTienda(TipoUsuarioCliente, idTipoImagen);
        }
    }
}



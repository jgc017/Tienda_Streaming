using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Services.General
{
    // Servicio para consultas genericas compartidas por varias vistas.
    public class General : IGeneral
    {
        private const string DominioRaiz = "SIN DATOS";
        private const int RolSuperUsuario = 1;
        private const string AccionVer = "Ver";
        private const string TipoFormulario = "Formulario";
        private const string TipoModulo = "Modulo";
        private readonly AppDbContext _context;
        private readonly ILogger<General> _logger;

        public General(AppDbContext context, ILogger<General> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Obtiene dominios disponibles para dropdowns, ocultando el registro raiz SIN DATOS.
        public async Task<List<DtoDominioDropdownItem>> ObtenerDominios()
        {
            return await _context.Dominios
                .AsNoTracking()
                .Where(d => d.Descripcion.ToUpper() != DominioRaiz)
                .OrderBy(d => d.Descripcion)
                .Select(d => new DtoDominioDropdownItem
                {
                    Id_Dominio = d.Id_Dominio,
                    Descripcion = d.Descripcion
                })
                .ToListAsync();
        }

        // Obtiene dominios hijos de un dominio padre. Si llega idSubDominio,
        // limita el resultado a una accion especifica, por ejemplo Ver.
        public async Task<List<DtoDominioDropdownItem>> ObtenerDominiosPorPadre(int idDominio, int? idSubDominio = null)
        {
            var query = _context.Dominios
                .AsNoTracking()
                .Where(d => d.Vigente == 1
                    && d.Id_Padre == idDominio
                    && d.Descripcion.ToUpper() != DominioRaiz);

            if (idSubDominio.HasValue)
            {
                query = query.Where(d => d.Id_Dominio == idSubDominio.Value);
            }

            return await query
                .OrderBy(d => d.Descripcion)
                .Select(d => new DtoDominioDropdownItem
                {
                    Id_Dominio = d.Id_Dominio,
                    Descripcion = d.Descripcion
                })
                .ToListAsync();
        }

        public async Task<List<DtoUsuarioDropdownItem>> ObtenerUsuarios()
        {
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Vigente == 1)
                .OrderBy(u => u.Nombre)
                .Select(u => new DtoUsuarioDropdownItem
                {
                    Id_Usuario = u.Id_Usuario,
                    Nombre = u.Nombre,
                    Usuario = u.Usuario
                })
                .ToListAsync();
        }

        // Obtiene menus activos y completos con una descripcion jerarquica para dropdowns.
        // Ejemplo: Administracion / Usuarios.
        public async Task<List<DtoMenuDropdownItem>> ObtenerMenus()
        {
            var menus = await _context.Menus
                .AsNoTracking()
                .Where(m => m.Vigente == 1)
                .OrderBy(m => m.Id_Padre ?? 0)
                .ThenBy(m => m.Posicion)
                .ThenBy(m => m.Descripcion)
                .Select(m => new
                {
                    m.Id_Menu,
                    m.Descripcion,
                    m.Id_Padre,
                    m.Posicion,
                    m.Controlador,
                    m.Vista
                })
                .ToListAsync();

            var resultado = new List<DtoMenuDropdownItem>();

            void AgregarNivel(int? idPadre, string prefijo)
            {
                foreach (var menu in menus.Where(m => m.Id_Padre == idPadre).OrderBy(m => m.Posicion).ThenBy(m => m.Descripcion))
                {
                    var descripcion = string.IsNullOrWhiteSpace(prefijo)
                        ? menu.Descripcion
                        : $"{prefijo} / {menu.Descripcion}";

                    if (MenuTieneRuta(menu.Controlador, menu.Vista))
                    {
                        resultado.Add(new DtoMenuDropdownItem
                        {
                            Id_Menu = menu.Id_Menu,
                            Descripcion = descripcion
                        });
                    }

                    AgregarNivel(menu.Id_Menu, descripcion);
                }
            }

            AgregarNivel(null, string.Empty);
            return resultado;
        }

        // F_GetMenu: construye el menu lateral segun los roles del usuario.
        // Regla fija: el rol 1 ve todo; los demas solo ven menus con permiso Ver.
        public async Task<List<DtoMenuSistemaItem>> F_GetMenu(IEnumerable<int> rolesUsuario)
        {
            var roles = rolesUsuario.Distinct().ToList();
            if (!roles.Any())
            {
                return new List<DtoMenuSistemaItem>();
            }

            var esSuperUsuario = roles.Contains(RolSuperUsuario);
            var menus = await _context.Menus
                .AsNoTracking()
                .Where(m => m.Vigente == 1)
                .Select(m => new DtoMenuSistemaItem
                {
                    Id_Menu = m.Id_Menu,
                    Descripcion = m.Descripcion,
                    Id_Padre = m.Id_Padre,
                    Posicion = m.Posicion,
                    Tipo = m.Tipo ?? string.Empty,
                    Controlador = m.Controlador,
                    Vista = m.Vista,
                    Icono = string.IsNullOrWhiteSpace(m.Icono) ? "fa-solid fa-circle" : m.Icono
                })
                .ToListAsync();

            var menusPorId = menus.ToDictionary(m => m.Id_Menu);
            var idsVisibles = esSuperUsuario
                ? menus.Select(m => m.Id_Menu).ToHashSet()
                : await ObtenerMenusAutorizados(roles);

            AgregarPadres(idsVisibles, menusPorId);

            var visibles = menus
                .Where(m => idsVisibles.Contains(m.Id_Menu))
                .Where(MenuPuedeParticipar)
                .ToList();

            return ConstruirJerarquia(visibles, null, 0);
        }

        private async Task<HashSet<int>> ObtenerMenusAutorizados(List<int> roles)
        {
            return (await _context.Roles_Permisos
                .AsNoTracking()
                .Where(rp => rp.Vigente == 1
                    && roles.Contains(rp.Id_Rol)
                    && rp.Permiso.Vigente == 1
                    && rp.Permiso.TipoPermiso == "Menu"
                    && rp.Permiso.Id_Menu.HasValue
                    && rp.Permiso.Accion.ToLower() == AccionVer.ToLower())
                .Select(rp => rp.Permiso.Id_Menu!.Value)
                .Distinct()
                .ToListAsync())
                .ToHashSet();
        }

        // TienePermisoMenu: valida acceso directo a una vista del menu.
        // El rol 1 tiene acceso total; los demas requieren permiso Ver vigente.
        public async Task<bool> TienePermisoMenu(IEnumerable<int> rolesUsuario, string controlador, string vista)
        {
            var roles = rolesUsuario.Distinct().ToList();
            if (!roles.Any())
            {
                return false;
            }

            if (roles.Contains(RolSuperUsuario))
            {
                return true;
            }

            var controladorNormalizado = controlador.Trim().ToLower();
            var vistaNormalizada = vista.Trim().ToLower();
            var idMenu = await _context.Menus
                .AsNoTracking()
                .Where(m => m.Vigente == 1
                    && m.Controlador != null
                    && m.Vista != null
                    && m.Controlador.ToLower() == controladorNormalizado
                    && m.Vista.ToLower() == vistaNormalizada)
                .Select(m => (int?)m.Id_Menu)
                .FirstOrDefaultAsync();

            if (!idMenu.HasValue)
            {
                idMenu = await _context.Menus
                    .AsNoTracking()
                    .Where(m => m.Vigente == 1
                        && m.Vista != null
                        && m.Vista.ToLower() == vistaNormalizada)
                    .OrderBy(m => m.Id_Menu)
                    .Select(m => (int?)m.Id_Menu)
                    .FirstOrDefaultAsync();
            }

            if (!idMenu.HasValue)
            {
                return false;
            }

            return await _context.Roles_Permisos
                .AsNoTracking()
                .AnyAsync(rp => rp.Vigente == 1
                    && roles.Contains(rp.Id_Rol)
                    && rp.Permiso.Vigente == 1
                    && rp.Permiso.TipoPermiso == "Menu"
                    && rp.Permiso.Id_Menu == idMenu.Value
                    && rp.Permiso.Accion.ToLower() == AccionVer.ToLower());
        }

        // TienePermisoMetodo: valida ejecucion de endpoints API por rol.
        // El rol 1 es super usuario y no requiere asignaciones.
        public async Task<bool> TienePermisoMetodo(IEnumerable<int> rolesUsuario, string controlador, string metodo, string httpMetodo)
        {
            var roles = rolesUsuario.Distinct().ToList();
            if (!roles.Any())
            {
                return false;
            }

            if (roles.Contains(RolSuperUsuario))
            {
                return true;
            }

            var controladorNormalizado = controlador.Trim().ToLower();
            var metodoNormalizado = metodo.Trim().ToLower();
            var httpMetodoNormalizado = httpMetodo.Trim().ToLower();

            return await _context.Roles_Permisos
                .AsNoTracking()
                .AnyAsync(rp => rp.Vigente == 1
                    && roles.Contains(rp.Id_Rol)
                    && rp.Permiso.Vigente == 1
                    && rp.Permiso.TipoPermiso == "Metodo"
                    && rp.Permiso.Controlador != null
                    && rp.Permiso.Metodo != null
                    && rp.Permiso.HttpMetodo != null
                    && rp.Permiso.Controlador.ToLower() == controladorNormalizado
                    && rp.Permiso.Metodo.ToLower() == metodoNormalizado
                    && (rp.Permiso.HttpMetodo.ToLower() == httpMetodoNormalizado ||
                        rp.Permiso.HttpMetodo.ToLower() == "any"));
        }

        private static void AgregarPadres(HashSet<int> idsVisibles, Dictionary<int, DtoMenuSistemaItem> menusPorId)
        {
            foreach (var idMenu in idsVisibles.ToList())
            {
                var actual = menusPorId.GetValueOrDefault(idMenu);
                while (actual?.Id_Padre != null && menusPorId.TryGetValue(actual.Id_Padre.Value, out var padre))
                {
                    idsVisibles.Add(padre.Id_Menu);
                    actual = padre;
                }
            }
        }

        private static bool MenuPuedeParticipar(DtoMenuSistemaItem menu)
        {
            if (EsFormulario(menu))
            {
                return menu.TieneRuta;
            }

            if (EsModulo(menu))
            {
                return true;
            }

            return menu.TieneRuta;
        }

        private static List<DtoMenuSistemaItem> ConstruirJerarquia(List<DtoMenuSistemaItem> menus, int? idPadre, int nivel)
        {
            var items = menus
                .Where(m => m.Id_Padre == idPadre)
                .OrderBy(m => m.Posicion)
                .ThenBy(m => m.Descripcion)
                .Select(m =>
                {
                    m.Nivel = nivel;
                    m.Hijos = ConstruirJerarquia(menus, m.Id_Menu, nivel + 1);
                    return m;
                })
                .Where(m => m.TieneRuta || m.Hijos.Any())
                .ToList();

            return items;
        }

        private static bool EsFormulario(DtoMenuSistemaItem menu)
        {
            return string.Equals(menu.Tipo, TipoFormulario, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsModulo(DtoMenuSistemaItem menu)
        {
            return string.Equals(menu.Tipo, TipoModulo, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MenuTieneRuta(string? controlador, string? vista)
        {
            return !string.IsNullOrWhiteSpace(controlador) && !string.IsNullOrWhiteSpace(vista);
        }

        // RegistrarAuditoria: inserta un evento transversal del sistema.
        // Se usa desde los controladores para registrar ingresos a formularios y ejecucion de metodos relevantes.
        public async Task RegistrarAuditoria(AuditContext audit, string formulario, string metodoEjecutado, string descripcion)
        {
            try
            {
                _context.Auditoria.Add(new Auditoria
                {
                    Formulario = Limitar(formulario, 120),
                    Metodo_Ejecutado = Limitar(metodoEjecutado, 120),
                    Descripcion = Limitar(descripcion, 500),
                    Vigente = 1,
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible registrar auditoria para {Formulario} - {Metodo}", formulario, metodoEjecutado);
            }
        }

        private static string Limitar(string value, int maxLength)
        {
            var text = value.Trim();
            return text.Length <= maxLength ? text : text[..maxLength];
        }
    }
}

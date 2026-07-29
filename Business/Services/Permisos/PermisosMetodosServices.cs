using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Permisos;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Dto.Administracion.Permisos;
using System.Reflection;

namespace Tienda_Streaming.Business.Services.Permisos
{
    // Servicio que sincroniza permisos de metodo a partir de los controladores API.
    // Evita crear permisos manualmente en codigo o directamente en base de datos.
    public class PermisosMetodosServices : IPermisosMetodos
    {
        private const string TipoPermisoMetodo = "Metodo";
        private readonly AppDbContext _context;
        private readonly IActionDescriptorCollectionProvider _actionProvider;
        private readonly ILogger<PermisosMetodosServices> _logger;

        public PermisosMetodosServices(
            AppDbContext context,
            IActionDescriptorCollectionProvider actionProvider,
            ILogger<PermisosMetodosServices> logger)
        {
            _context = context;
            _actionProvider = actionProvider;
            _logger = logger;
        }

        // F_GetPermisosMetodosList: consulta permisos de metodos para la grilla.
        public async Task<ServiceResult> F_GetPermisosMetodosList()
        {
            var permisos = await _context.Permisos
                .AsNoTracking()
                .Include(p => p.Menu)
                .Where(p => p.TipoPermiso == TipoPermisoMetodo
                    && p.Metodo != null
                    && !p.Metodo.EndsWith("List")
                    && !p.Metodo.Contains("Dropdown"))
                .OrderBy(p => p.Menu != null ? p.Menu.Vista : p.Modulo)
                .ThenBy(p => p.Controlador)
                .ThenBy(p => p.Metodo)
                .ThenBy(p => p.HttpMetodo)
                .Select(p => new
                {
                    p.Id_Permiso,
                    p.Id_Menu,
                    Vista = p.Menu != null ? p.Menu.Vista : null,
                    Formulario = p.Menu != null ? p.Menu.Descripcion : null,
                    p.Modulo,
                    p.Accion,
                    p.Descripcion,
                    p.Controlador,
                    p.Metodo,
                    p.HttpMetodo,
                    p.CodigoPermiso,
                    p.Vigente,
                    p.Fecha_Creacion
                })
                .ToListAsync();

            return ServiceResult.Success(data: permisos);
        }

        // F_GetPermisoMetodo: consulta un permiso de metodo por id para editar
        // descripcion y estado sin modificar metadata tecnica.
        public async Task<ServiceResult> F_GetPermisoMetodo(int idPermiso)
        {
            var permiso = await _context.Permisos
                .AsNoTracking()
                .Include(p => p.Menu)
                .FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso && p.TipoPermiso == TipoPermisoMetodo);

            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso de metodo no encontrado.");
            }

            return ServiceResult.Success(data: new
            {
                permiso.Id_Permiso,
                permiso.Id_Menu,
                Vista = permiso.Menu?.Vista,
                Formulario = permiso.Menu?.Descripcion,
                permiso.Modulo,
                permiso.Accion,
                permiso.Descripcion,
                permiso.Controlador,
                permiso.Metodo,
                permiso.HttpMetodo,
                permiso.CodigoPermiso,
                permiso.Vigente
            }, auditDescription: $"Consulta del permiso de metodo {permiso.CodigoPermiso} con id {permiso.Id_Permiso}");
        }

        // P_SyncPermisosMetodos: escanea controladores API y crea/actualiza
        // permisos de metodo segun las rutas reales expuestas por MVC.
        public async Task<ServiceResult> P_SyncPermisosMetodos(AuditContext audit)
        {
            var indiceCompatible = await AsegurarIndicePermisosMetodoCompatible();
            if (!indiceCompatible.Ok)
            {
                return indiceCompatible;
            }

            var metodos = ObtenerMetodosApi();
            var menus = await _context.Menus
                .AsNoTracking()
                .Where(m => m.Vigente == 1 && m.Tipo == "Formulario" && m.Controlador != null && m.Vista != null)
                .ToListAsync();
            AsociarMenus(metodos, menus);

            var codigosActuales = metodos.Select(m => m.CodigoPermiso).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existentes = await _context.Permisos
                .Where(p => p.TipoPermiso == TipoPermisoMetodo)
                .ToListAsync();

            var existentesPorCodigo = existentes
                .Where(p => !string.IsNullOrWhiteSpace(p.CodigoPermiso))
                .ToDictionary(p => p.CodigoPermiso!, StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var creados = 0;
            var actualizados = 0;
            var inactivados = 0;

            foreach (var metodo in metodos)
            {
                if (!existentesPorCodigo.TryGetValue(metodo.CodigoPermiso, out var permiso))
                {
                    _context.Permisos.Add(new Models.Administracion.Permisos
                    {
                        TipoPermiso = TipoPermisoMetodo,
                        Id_Menu = metodo.Id_Menu,
                        Modulo = metodo.Modulo,
                        Accion = metodo.Accion,
                        Descripcion = metodo.Descripcion,
                        Controlador = metodo.Controlador,
                        Metodo = metodo.Metodo,
                        HttpMetodo = metodo.HttpMetodo,
                        CodigoPermiso = metodo.CodigoPermiso,
                        Vigente = 1,
                        Id_Usuario_Creacion = audit.UserId,
                        Fecha_Creacion = now,
                        Maquina_Creacion = audit.Machine
                    });
                    creados++;
                    continue;
                }

                var cambio = permiso.Modulo != metodo.Modulo ||
                    permiso.Id_Menu != metodo.Id_Menu ||
                    permiso.Accion != metodo.Accion ||
                    permiso.Controlador != metodo.Controlador ||
                    permiso.Metodo != metodo.Metodo ||
                    permiso.HttpMetodo != metodo.HttpMetodo ||
                    DebeActualizarDescripcion(permiso.Descripcion);

                if (!cambio)
                {
                    continue;
                }

                permiso.Modulo = metodo.Modulo;
                permiso.Id_Menu = metodo.Id_Menu;
                permiso.Accion = metodo.Accion;
                if (DebeActualizarDescripcion(permiso.Descripcion))
                {
                    permiso.Descripcion = metodo.Descripcion;
                }
                permiso.Controlador = metodo.Controlador;
                permiso.Metodo = metodo.Metodo;
                permiso.HttpMetodo = metodo.HttpMetodo;
                permiso.Id_Usuario_Modifica = audit.UserId;
                permiso.Fecha_Modifica = now;
                permiso.Maquina_Modifica = audit.Machine;
                actualizados++;
            }

            foreach (var permiso in existentes.Where(p => !string.IsNullOrWhiteSpace(p.CodigoPermiso) && !codigosActuales.Contains(p.CodigoPermiso!)))
            {
                if (permiso.Vigente == 0)
                {
                    continue;
                }

                permiso.Vigente = 0;
                permiso.Id_Usuario_Modifica = audit.UserId;
                permiso.Fecha_Modifica = now;
                permiso.Maquina_Modifica = audit.Machine;
                inactivados++;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto sincronizando permisos de metodos");
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "No fue posible sincronizar los permisos de metodos.");
            }

            return ServiceResult.Success(
                $"Sincronizacion completada. Creados: {creados}, actualizados: {actualizados}, inactivados: {inactivados}.",
                auditDescription: $"Sincronizacion de permisos de metodos. Creados: {creados}, actualizados: {actualizados}, inactivados: {inactivados}");
        }

        private async Task<ServiceResult> AsegurarIndicePermisosMetodoCompatible()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("""
                    DROP INDEX IF EXISTS "IX_Permisos_TipoPermiso_Id_Menu_Accion";

                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Permisos_TipoPermiso_Id_Menu_Accion"
                    ON "Permisos" ("TipoPermiso", "Id_Menu", "Accion")
                    WHERE "Id_Menu" IS NOT NULL AND "TipoPermiso" = 'Menu';
                    """);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No fue posible ajustar el indice de permisos antes de sincronizar metodos");
                return ServiceResult.Fail(
                    StatusCodes.Status409Conflict,
                    "No fue posible preparar la base para sincronizar permisos de metodos. Verifica permisos del usuario de base de datos sobre indices.");
            }
        }

        // P_UdpPermisoMetodo: actualiza descripcion y estado del permiso.
        public async Task<ServiceResult> P_UdpPermisoMetodo(int idPermiso, DtoPermisoMetodoUpdateRequest model, AuditContext audit)
        {
            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso && p.TipoPermiso == TipoPermisoMetodo);

            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso de metodo no existe.");
            }

            permiso.Descripcion = model.Descripcion?.Trim();
            permiso.Vigente = model.Vigente;
            permiso.Id_Usuario_Modifica = audit.UserId;
            permiso.Fecha_Modifica = DateTime.UtcNow;
            permiso.Maquina_Modifica = audit.Machine;

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Permiso de metodo actualizado correctamente.",
                auditDescription: $"Actualizacion del permiso de metodo {permiso.CodigoPermiso} con id {permiso.Id_Permiso}");
        }

        // P_DeletePermisoMetodo: baja logica del permiso de metodo y de sus
        // asignaciones activas a roles.
        public async Task<ServiceResult> P_DeletePermisoMetodo(int idPermiso, AuditContext audit)
        {
            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso && p.TipoPermiso == TipoPermisoMetodo);

            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso de metodo no existe.");
            }

            var now = DateTime.UtcNow;
            permiso.Vigente = 0;
            permiso.Id_Usuario_Modifica = audit.UserId;
            permiso.Fecha_Modifica = now;
            permiso.Maquina_Modifica = audit.Machine;

            var asignaciones = await _context.Roles_Permisos
                .Where(rp => rp.Id_Permiso == idPermiso && rp.Vigente == 1)
                .ToListAsync();

            foreach (var asignacion in asignaciones)
            {
                asignacion.Vigente = 0;
                asignacion.Id_Usuario_Modifica = audit.UserId;
                asignacion.Fecha_Modifica = now;
                asignacion.Maquina_Modifica = audit.Machine;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Permiso de metodo marcado como inactivo correctamente.",
                auditDescription: $"Eliminacion logica del permiso de metodo {permiso.CodigoPermiso} con id {permiso.Id_Permiso}");
        }

        private List<MetodoApiDetectado> ObtenerMetodosApi()
        {
            return _actionProvider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .Where(EsMetodoApiProtegido)
                .Where(action => !EsMetodoEstandar(action.ActionName))
                .SelectMany(CrearPermisosDesdeAction)
                .GroupBy(m => m.CodigoPermiso, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(m => m.Controlador)
                .ThenBy(m => m.Metodo)
                .ToList();
        }

        private static bool EsMetodoApiProtegido(ControllerActionDescriptor action)
        {
            var ns = action.ControllerTypeInfo.Namespace ?? string.Empty;
            if (!ns.Contains(".Controllers.Api", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !TieneAtributo<AllowAnonymousAttribute>(action.ControllerTypeInfo) &&
                   !TieneAtributo<AllowAnonymousAttribute>(action.MethodInfo);
        }

        private static IEnumerable<MetodoApiDetectado> CrearPermisosDesdeAction(ControllerActionDescriptor action)
        {
            var httpMethods = action.EndpointMetadata
                .OfType<IActionHttpMethodProvider>()
                .SelectMany(m => m.HttpMethods)
                .DefaultIfEmpty("ANY")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(m => m)
                .ToList();

            foreach (var httpMethod in httpMethods)
            {
                var controlador = action.ControllerName;
                var metodo = action.ActionName;
                var modulo = ResolverModulo(controlador);
                var accion = ResolverAccion(metodo, httpMethod);
                var codigo = $"{controlador}.{metodo}.{httpMethod}".ToUpperInvariant();

                yield return new MetodoApiDetectado
                {
                    Modulo = modulo,
                    Accion = accion,
                    Controlador = controlador,
                    Metodo = metodo,
                    HttpMetodo = httpMethod,
                    CodigoPermiso = codigo,
                    Descripcion = CrearDescripcionLegible(modulo, accion, metodo)
                };
            }
        }

        private static void AsociarMenus(IEnumerable<MetodoApiDetectado> metodos, IEnumerable<Models.Administracion.Menus> menus)
        {
            var menusPorControlador = menus
                .Where(m => !string.IsNullOrWhiteSpace(m.Controlador))
                .GroupBy(m => m.Controlador!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Posicion).First(), StringComparer.OrdinalIgnoreCase);

            var menusPorVista = menus
                .Where(m => !string.IsNullOrWhiteSpace(m.Vista))
                .GroupBy(m => m.Vista!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.Posicion).First(), StringComparer.OrdinalIgnoreCase);

            foreach (var metodo in metodos)
            {
                if (menusPorControlador.TryGetValue(metodo.Modulo, out var menu))
                {
                    metodo.Id_Menu = menu.Id_Menu;
                    continue;
                }

                if (menusPorVista.TryGetValue(ResolverVistaMenu(metodo.Modulo), out menu))
                {
                    metodo.Id_Menu = menu.Id_Menu;
                    continue;
                }

                if (menusPorControlador.TryGetValue(ResolverModulo(metodo.Controlador), out menu))
                {
                    metodo.Id_Menu = menu.Id_Menu;
                }
            }
        }

        private static string ResolverModulo(string controlador)
        {
            var sinSufijoApi = controlador.EndsWith("Api", StringComparison.OrdinalIgnoreCase)
                ? controlador[..^3]
                : controlador;

            return ResolverControladorMenu(sinSufijoApi);
        }

        private static string ResolverControladorMenu(string controlador)
        {
            return controlador switch
            {
                "RolesUser" => "Usuarios",
                "TiendaInterna" => "Tiendas",
                "PermisosMetodos" => "Permisos",
                _ => controlador
            };
        }

        private static string ResolverVistaMenu(string modulo)
        {
            return $"Vw{modulo}";
        }

        private static string ResolverAccion(string metodo, string httpMethod)
        {
            if (metodo.StartsWith("P_Sync", StringComparison.OrdinalIgnoreCase))
            {
                return "Sincronizar";
            }

            if (metodo.StartsWith("P_Upload", StringComparison.OrdinalIgnoreCase))
            {
                return "Cargar";
            }

            if (metodo.Contains("Restaurar", StringComparison.OrdinalIgnoreCase) ||
                metodo.Contains("Restore", StringComparison.OrdinalIgnoreCase))
            {
                return "Restaurar";
            }

            if (metodo.Contains("Asignar", StringComparison.OrdinalIgnoreCase) ||
                metodo.Contains("PermisoRol", StringComparison.OrdinalIgnoreCase) ||
                metodo.Contains("RolesPermiso", StringComparison.OrdinalIgnoreCase) ||
                metodo.Contains("UsuarioRoles", StringComparison.OrdinalIgnoreCase))
            {
                return "Asignar";
            }

            if (metodo.StartsWith("P_Ins", StringComparison.OrdinalIgnoreCase) || httpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                return "Crear";
            }

            if (metodo.StartsWith("P_Udp", StringComparison.OrdinalIgnoreCase) || httpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                return metodo.Contains("Rol", StringComparison.OrdinalIgnoreCase) ||
                       metodo.Contains("Asignar", StringComparison.OrdinalIgnoreCase)
                    ? "Asignar"
                    : "Actualizar";
            }

            if (metodo.StartsWith("P_Delete", StringComparison.OrdinalIgnoreCase) || httpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                return "Eliminar";
            }

            if (metodo.StartsWith("F_Get", StringComparison.OrdinalIgnoreCase) || httpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                return "Consultar";
            }

            return "Ejecutar";
        }

        private static bool EsMetodoEstandar(string metodo)
        {
            return metodo.EndsWith("List", StringComparison.OrdinalIgnoreCase) ||
                   metodo.Contains("Dropdown", StringComparison.OrdinalIgnoreCase);
        }

        private static string CrearDescripcionLegible(string modulo, string accion, string metodo)
        {
            var moduloLegible = SepararCamelCase(modulo);

            if (metodo.Contains("Imagen", StringComparison.OrdinalIgnoreCase) ||
                metodo.StartsWith("P_Upload", StringComparison.OrdinalIgnoreCase))
            {
                return $"Permite cargar archivos o imagenes del modulo {moduloLegible}.";
            }

            return accion switch
            {
                "Crear" => $"Permite registrar nueva informacion en el modulo {moduloLegible}.",
                "Consultar" => $"Permite consultar el detalle de registros del modulo {moduloLegible}.",
                "Actualizar" => $"Permite modificar informacion existente del modulo {moduloLegible}.",
                "Eliminar" => $"Permite eliminar o inactivar registros del modulo {moduloLegible}.",
                "Asignar" => $"Permite asignar o actualizar relaciones del modulo {moduloLegible}.",
                "Restaurar" => $"Permite restaurar credenciales o informacion sensible del modulo {moduloLegible}.",
                "Sincronizar" => $"Permite sincronizar informacion automatica del modulo {moduloLegible}.",
                "Cargar" => $"Permite cargar archivos o informacion externa en el modulo {moduloLegible}.",
                _ => $"Permite ejecutar una accion administrativa del modulo {moduloLegible}."
            };
        }

        private static bool DebeActualizarDescripcion(string? descripcion)
        {
            return string.IsNullOrWhiteSpace(descripcion) ||
                   descripcion.StartsWith("Permite ejecutar ", StringComparison.OrdinalIgnoreCase);
        }

        private static string SepararCamelCase(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return "General";
            }

            var caracteres = new List<char>();
            for (var i = 0; i < valor.Length; i++)
            {
                if (i > 0 && char.IsUpper(valor[i]) && !char.IsWhiteSpace(valor[i - 1]))
                {
                    caracteres.Add(' ');
                }

                caracteres.Add(valor[i]);
            }

            return new string(caracteres.ToArray());
        }

        private static bool TieneAtributo<T>(MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo.GetCustomAttributes(typeof(T), inherit: true).Any();
        }

        private sealed class MetodoApiDetectado
        {
            public int? Id_Menu { get; set; }
            public string Modulo { get; set; } = string.Empty;
            public string Accion { get; set; } = string.Empty;
            public string Controlador { get; set; } = string.Empty;
            public string Metodo { get; set; } = string.Empty;
            public string HttpMetodo { get; set; } = string.Empty;
            public string CodigoPermiso { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
        }
    }
}

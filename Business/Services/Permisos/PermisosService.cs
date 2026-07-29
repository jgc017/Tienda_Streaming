using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Permisos;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Permisos;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Services.Permisos
{
    // Servicio de negocio para el CRUD de permisos.
    public class PermisosService : IPermisos
    {
        private const int RolSuperUsuario = 1;
        private const string TipoPermisoMenu = "Menu";
        private readonly AppDbContext _context;
        private readonly ILogger<PermisosService> _logger;

        public PermisosService(AppDbContext context, ILogger<PermisosService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // P_InsPermiso: registra una accion autorizable dentro de un modulo.
        public async Task<ServiceResult> P_InsPermiso(DtoPermisoCreateRequest model, AuditContext audit)
        {
            var menu = await ObtenerMenuValido(model.Id_Menu);
            if (menu == null)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El menu seleccionado no existe, esta inactivo o no tiene ruta valida.");
            }

            var modulo = menu.Descripcion.Trim();
            var accion = model.Accion.Trim();
            var accionNormalizada = accion.ToLowerInvariant();

            var existe = await _context.Permisos
                .AnyAsync(p => p.TipoPermiso == TipoPermisoMenu &&
                               p.Id_Menu == menu.Id_Menu &&
                               p.Accion.ToLower() == accionNormalizada);

            if (existe)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un permiso para ese menu y accion.");
            }

            var permiso = new Models.Administracion.Permisos
            {
                Id_Menu = menu.Id_Menu,
                TipoPermiso = TipoPermisoMenu,
                Modulo = modulo,
                Accion = accion,
                Descripcion = model.Descripcion?.Trim(),
                Vigente = 1,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.Permisos.Add(permiso);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto registrando permiso {Modulo}.{Accion}", modulo, accion);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un permiso para ese menu y accion.");
            }

            return ServiceResult.Success(
                "Permiso registrado correctamente.",
                auditDescription: $"Registro del permiso {permiso.Modulo}.{permiso.Accion} con id {permiso.Id_Permiso}");
        }

        // F_GetPermisosList: consulta permisos para alimentar la grilla principal.
        public async Task<ServiceResult> F_GetPermisosList()
        {
            var permisos = await _context.Permisos
                .AsNoTracking()
                .Include(p => p.Menu)
                .Where(p => p.TipoPermiso == TipoPermisoMenu)
                .OrderBy(p => p.Modulo)
                .ThenBy(p => p.Accion)
                .Select(p => new
                {
                    p.Id_Permiso,
                    p.Id_Menu,
                    Menu = p.Menu != null ? p.Menu.Descripcion : p.Modulo,
                    p.Modulo,
                    p.Accion,
                    p.Descripcion,
                    p.Vigente,
                    p.Fecha_Creacion
                })
                .ToListAsync();

            return ServiceResult.Success(data: permisos);
        }

        // F_GetPermiso: consulta un permiso por id para cargar el modal de actualizacion.
        public async Task<ServiceResult> F_GetPermiso(int idPermiso)
        {
            var permiso = await _context.Permisos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso);

            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso no encontrado.");
            }

            return ServiceResult.Success(data: new
            {
                permiso.Id_Permiso,
                permiso.Id_Menu,
                permiso.Modulo,
                permiso.Accion,
                permiso.Descripcion,
                permiso.Vigente
            }, auditDescription: $"Consulta del permiso {permiso.Modulo}.{permiso.Accion} con id {permiso.Id_Permiso}");
        }

        // P_UdpPermiso: actualiza modulo, accion, descripcion y estado vigente.
        public async Task<ServiceResult> P_UdpPermiso(int idPermiso, DtoPermisoUpdateRequest model, AuditContext audit)
        {
            var permiso = await _context.Permisos.FindAsync(idPermiso);
            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso no existe.");
            }

            var menu = await ObtenerMenuValido(model.Id_Menu);
            if (menu == null)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El menu seleccionado no existe, esta inactivo o no tiene ruta valida.");
            }

            var modulo = menu.Descripcion.Trim();
            var accion = model.Accion.Trim();
            var accionNormalizada = accion.ToLowerInvariant();

            var duplicado = await _context.Permisos
                .AnyAsync(p => p.Id_Permiso != idPermiso &&
                    p.TipoPermiso == TipoPermisoMenu &&
                    p.Id_Menu == menu.Id_Menu &&
                    p.Accion.ToLower() == accionNormalizada);

            if (duplicado)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un permiso para ese menu y accion.");
            }

            permiso.Id_Menu = menu.Id_Menu;
            permiso.TipoPermiso = TipoPermisoMenu;
            permiso.Modulo = modulo;
            permiso.Accion = accion;
            permiso.Descripcion = model.Descripcion?.Trim();
            permiso.Vigente = model.Vigente;
            permiso.Id_Usuario_Modifica = audit.UserId;
            permiso.Fecha_Modifica = DateTime.UtcNow;
            permiso.Maquina_Modifica = audit.Machine;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto actualizando permiso {IdPermiso}", idPermiso);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un permiso para ese menu y accion.");
            }

            return ServiceResult.Success(
                "Permiso actualizado correctamente.",
                auditDescription: $"Actualizacion del permiso {permiso.Modulo}.{permiso.Accion} con id {permiso.Id_Permiso}");
        }

        // P_DeletePermiso: baja logica del permiso y de sus asignaciones activas.
        public async Task<ServiceResult> P_DeletePermiso(int idPermiso, AuditContext audit)
        {
            try
            {
                var permiso = await _context.Permisos.FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso);
                if (permiso == null)
                {
                    return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso no existe.");
                }

                var now = DateTime.UtcNow;

                if (permiso.Vigente != 0)
                {
                    permiso.Vigente = 0;
                    permiso.Id_Usuario_Modifica = audit.UserId;
                    permiso.Fecha_Modifica = now;
                    permiso.Maquina_Modifica = audit.Machine;
                }

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
                    "Permiso marcado como inactivo correctamente.",
                    auditDescription: $"Eliminacion logica del permiso {permiso.Modulo}.{permiso.Accion} con id {permiso.Id_Permiso}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando permiso {IdPermiso}", idPermiso);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "Ocurrio un error al eliminar el permiso.");
            }
        }

        // F_GetRolesAsignables: lista roles activos excluyendo el rol 1.
        // El super usuario ve todo y no necesita asignaciones de permisos.
        public async Task<List<DtoRolPermisoDropdownItem>> F_GetRolesAsignables()
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(r => r.Vigente == 1 && r.Id_Rol != RolSuperUsuario)
                .OrderBy(r => r.Rol)
                .Select(r => new DtoRolPermisoDropdownItem
                {
                    Id_Rol = r.Id_Rol,
                    Rol = r.Rol
                })
                .ToListAsync();
        }

        // F_GetRolesPorPermiso: trae todos los roles asignables y marca si el permiso esta activo para cada rol.
        public async Task<ServiceResult> F_GetRolesPorPermiso(int idPermiso)
        {
            var permiso = await _context.Permisos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso);

            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso no existe.");
            }

            if (permiso.Vigente != 1)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El permiso esta inactivo.");
            }

            var asignaciones = await _context.Roles_Permisos
                .AsNoTracking()
                .Where(rp => rp.Id_Permiso == idPermiso)
                .Select(rp => new
                {
                    rp.Id_Rol_Permiso,
                    rp.Id_Rol,
                    rp.Vigente
                })
                .ToListAsync();

            var asignacionesPorRol = asignaciones
                .GroupBy(rp => rp.Id_Rol)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.OrderByDescending(rp => rp.Id_Rol_Permiso).First());

            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => r.Vigente == 1 && r.Id_Rol != RolSuperUsuario)
                .OrderBy(r => r.Rol)
                .Select(r => new
                {
                    r.Id_Rol,
                    r.Rol
                })
                .ToListAsync();

            var data = roles.Select(rol =>
            {
                asignacionesPorRol.TryGetValue(rol.Id_Rol, out var asignacion);

                return new DtoRolPorPermisoItem
                {
                    Id_Rol = rol.Id_Rol,
                    Rol = rol.Rol,
                    Id_Rol_Permiso = asignacion?.Id_Rol_Permiso,
                    Vigente = asignacion?.Vigente ?? 0,
                    Asignado = asignacion != null
                };
            }).ToList();

            return ServiceResult.Success(data: data);
        }

        // P_UdpRolesPermiso: guarda en lote que roles tienen activo o inactivo un permiso.
        public async Task<ServiceResult> P_UdpRolesPermiso(int idPermiso, DtoPermisoRolBulkUpdateRequest model, AuditContext audit)
        {
            var permiso = await _context.Permisos
                .FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso);

            if (permiso == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso no existe.");
            }

            if (permiso.Vigente != 1)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El permiso esta inactivo.");
            }

            var rolesSolicitados = model.Roles ?? new List<DtoPermisoRolBulkItem>();
            var rolesDuplicados = rolesSolicitados
                .GroupBy(r => r.Id_Rol)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (rolesDuplicados.Any())
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "La lista contiene roles duplicados.");
            }

            var idsRoles = rolesSolicitados.Select(r => r.Id_Rol).ToList();
            var rolesValidos = await _context.Roles
                .AsNoTracking()
                .Where(r => idsRoles.Contains(r.Id_Rol) && r.Vigente == 1 && r.Id_Rol != RolSuperUsuario)
                .Select(r => new
                {
                    r.Id_Rol,
                    r.Rol
                })
                .ToListAsync();

            if (rolesValidos.Count != idsRoles.Count)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Uno o mas roles no existen, estan inactivos o no son asignables.");
            }

            var asignaciones = await _context.Roles_Permisos
                .Where(rp => rp.Id_Permiso == idPermiso && idsRoles.Contains(rp.Id_Rol))
                .ToListAsync();

            var asignacionesPorRol = asignaciones
                .GroupBy(rp => rp.Id_Rol)
                .ToDictionary(
                    grupo => grupo.Key,
                    grupo => grupo.OrderByDescending(rp => rp.Id_Rol_Permiso).First());

            var rolesPorId = rolesValidos.ToDictionary(r => r.Id_Rol, r => r.Rol);
            var now = DateTime.UtcNow;
            var rolesActivados = new List<string>();
            var rolesInactivados = new List<string>();

            foreach (var item in rolesSolicitados)
            {
                var vigente = item.Vigente == 1 ? (short)1 : (short)0;
                asignacionesPorRol.TryGetValue(item.Id_Rol, out var asignacion);

                if (asignacion == null)
                {
                    if (vigente == 0)
                    {
                        continue;
                    }

                    _context.Roles_Permisos.Add(new Roles_Permisos
                    {
                        Id_Permiso = idPermiso,
                        Id_Rol = item.Id_Rol,
                        Vigente = 1,
                        Id_Usuario_Creacion = audit.UserId,
                        Fecha_Creacion = now,
                        Maquina_Creacion = audit.Machine
                    });

                    rolesActivados.Add(rolesPorId[item.Id_Rol]);
                    continue;
                }

                if (asignacion.Vigente == vigente)
                {
                    continue;
                }

                asignacion.Vigente = vigente;
                asignacion.Id_Usuario_Modifica = audit.UserId;
                asignacion.Fecha_Modifica = now;
                asignacion.Maquina_Modifica = audit.Machine;

                if (vigente == 1)
                {
                    rolesActivados.Add(rolesPorId[item.Id_Rol]);
                }
                else
                {
                    rolesInactivados.Add(rolesPorId[item.Id_Rol]);
                }
            }

            await _context.SaveChangesAsync();

            var totalCambios = rolesActivados.Count + rolesInactivados.Count;
            var mensaje = totalCambios == 0
                ? "No habia cambios por guardar."
                : "Permisos por rol actualizados correctamente.";

            var auditoria = totalCambios == 0
                ? null
                : $"Actualizacion masiva del permiso {permiso.Modulo}.{permiso.Accion} con id {permiso.Id_Permiso}. Roles activados: {string.Join(", ", rolesActivados)}. Roles inactivados: {string.Join(", ", rolesInactivados)}";

            return ServiceResult.Success(mensaje, auditDescription: auditoria);
        }

        // P_InsPermisoRol: asigna un permiso vigente a un rol vigente.
        // Si la relacion existia inactiva, se reactiva para no duplicar registros.
        public async Task<ServiceResult> P_InsPermisoRol(int idPermiso, DtoPermisoRolCreateRequest model, AuditContext audit)
        {
            var datos = await ValidarAsignacionPermisoRol(idPermiso, model.Id_Rol);
            if (datos.Error != null)
            {
                return datos.Error;
            }

            var asignacion = await _context.Roles_Permisos
                .FirstOrDefaultAsync(rp => rp.Id_Permiso == idPermiso && rp.Id_Rol == model.Id_Rol);

            if (asignacion != null && asignacion.Vigente == 1)
            {
                return ServiceResult.Success("El rol ya tenia este permiso activo.");
            }

            var now = DateTime.UtcNow;

            if (asignacion == null)
            {
                asignacion = new Roles_Permisos
                {
                    Id_Permiso = idPermiso,
                    Id_Rol = model.Id_Rol,
                    Vigente = 1,
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = now,
                    Maquina_Creacion = audit.Machine
                };

                _context.Roles_Permisos.Add(asignacion);
            }
            else
            {
                asignacion.Vigente = 1;
                asignacion.Id_Usuario_Modifica = audit.UserId;
                asignacion.Fecha_Modifica = now;
                asignacion.Maquina_Modifica = audit.Machine;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto asignando permiso {IdPermiso} al rol {IdRol}", idPermiso, model.Id_Rol);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "No fue posible asignar el permiso al rol.");
            }

            return ServiceResult.Success(
                "Permiso asignado al rol correctamente.",
                auditDescription: $"Asignacion del permiso {datos.Permiso!.Modulo}.{datos.Permiso.Accion} con id {datos.Permiso.Id_Permiso} al rol {datos.Rol!.Rol} con id {datos.Rol.Id_Rol}");
        }

        // F_GetPermisoRol: consulta si el rol ya tiene el permiso y en que estado.
        public async Task<ServiceResult> F_GetPermisoRol(int idPermiso, int idRol)
        {
            var datos = await ValidarAsignacionPermisoRol(idPermiso, idRol);
            if (datos.Error != null)
            {
                return datos.Error;
            }

            var asignacion = await _context.Roles_Permisos
                .AsNoTracking()
                .FirstOrDefaultAsync(rp => rp.Id_Permiso == idPermiso && rp.Id_Rol == idRol);

            if (asignacion == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "El rol no tiene este permiso asignado.");
            }

            return ServiceResult.Success(data: new
            {
                asignacion.Id_Rol_Permiso,
                asignacion.Id_Rol,
                asignacion.Id_Permiso,
                asignacion.Vigente,
                Rol = datos.Rol!.Rol,
                Permiso = $"{datos.Permiso!.Modulo}.{datos.Permiso.Accion}"
            }, auditDescription: $"Consulta de asignacion del permiso {datos.Permiso!.Modulo}.{datos.Permiso.Accion} con id {datos.Permiso.Id_Permiso} al rol {datos.Rol!.Rol} con id {datos.Rol.Id_Rol}");
        }

        // P_DeletePermisoRol: desasigna el permiso del rol usando baja logica.
        public async Task<ServiceResult> P_DeletePermisoRol(int idPermiso, int idRol, AuditContext audit)
        {
            var datos = await ValidarAsignacionPermisoRol(idPermiso, idRol);
            if (datos.Error != null)
            {
                return datos.Error;
            }

            var asignacion = await _context.Roles_Permisos
                .FirstOrDefaultAsync(rp => rp.Id_Permiso == idPermiso && rp.Id_Rol == idRol);

            if (asignacion == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "El rol no tiene este permiso asignado.");
            }

            asignacion.Vigente = 0;
            asignacion.Id_Usuario_Modifica = audit.UserId;
            asignacion.Fecha_Modifica = DateTime.UtcNow;
            asignacion.Maquina_Modifica = audit.Machine;

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Permiso desasignado del rol correctamente.",
                auditDescription: $"Desasignacion del permiso {datos.Permiso!.Modulo}.{datos.Permiso.Accion} con id {datos.Permiso.Id_Permiso} al rol {datos.Rol!.Rol} con id {datos.Rol.Id_Rol}");
        }

        private async Task<Menus?> ObtenerMenuValido(int? idMenu)
        {
            if (!idMenu.HasValue)
            {
                return null;
            }

            return await _context.Menus
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id_Menu == idMenu.Value
                    && m.Vigente == 1
                    && m.Controlador != null
                    && m.Controlador != ""
                    && m.Vista != null
                    && m.Vista != "");
        }

        private async Task<(Models.Administracion.Permisos? Permiso, Models.Administracion.Roles? Rol, ServiceResult? Error)> ValidarAsignacionPermisoRol(int idPermiso, int idRol)
        {
            var permiso = await _context.Permisos.AsNoTracking().FirstOrDefaultAsync(p => p.Id_Permiso == idPermiso);
            if (permiso == null)
            {
                return (null, null, ServiceResult.Fail(StatusCodes.Status404NotFound, "Permiso no existe."));
            }

            if (permiso.Vigente != 1)
            {
                return (permiso, null, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El permiso esta inactivo."));
            }

            var rol = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id_Rol == idRol);
            if (rol == null)
            {
                return (permiso, null, ServiceResult.Fail(StatusCodes.Status404NotFound, "Rol no existe."));
            }

            if (rol.Id_Rol == RolSuperUsuario)
            {
                return (permiso, rol, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El super usuario no requiere permisos asignados."));
            }

            if (rol.Vigente != 1)
            {
                return (permiso, rol, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El rol esta inactivo."));
            }

            return (permiso, rol, null);
        }

    }
}

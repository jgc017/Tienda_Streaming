using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.RolesUser;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.RolesUser;
using System;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Services.RolesUser
{
    // Servicio de negocio para consultar y sincronizar roles asignados a usuarios.
    public class RolesUserService : IRolesUser
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RolesUserService> _logger;

        public RolesUserService(AppDbContext context, ILogger<RolesUserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GetIdUserRoles: devuelve datos del usuario y roles activos indicando asignacion actual.
        public async Task<ServiceResult> GetIdUserRoles(int idUsuario)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id_Usuario == idUsuario)
                .Select(u => new
                {
                    u.Id_Usuario,
                    u.Nombre,
                    u.E_Mail
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "No se encontro el usuario.");
            }

            var roles = await (from rol in _context.Roles.AsNoTracking()
                               where rol.Vigente == 1
                               orderby rol.Rol
                               select new
                               {
                                   rol.Id_Rol,
                                   rol.Rol,
                                   Asignado = _context.Roles_User
                                       .Any(ru => ru.Id_Usuario == idUsuario &&
                                           ru.Id_Rol == rol.Id_Rol &&
                                           ru.Vigente == 1)
                               })
                .ToListAsync();

            return ServiceResult.Success(data: new
            {
                usuario.Id_Usuario,
                usuario.Nombre,
                usuario.E_Mail,
                Roles = roles
            }, auditDescription: $"Consulta de roles del usuario {usuario.Nombre} con id {usuario.Id_Usuario}");
        }

        // Asignar: sincroniza las asignaciones activas con la seleccion recibida desde el modal.
        public async Task<ServiceResult> Asignar(int idUsuario, DtoRolesUserUpdateRequest model, AuditContext audit)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.Id_Usuario == idUsuario)
                .Select(u => new { u.Id_Usuario, u.Nombre })
                .FirstOrDefaultAsync();

            if (usuario == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "El usuario no existe.");
            }

            var rolesSeleccionados = model.RoleIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (rolesSeleccionados.Count > 0)
            {
                var rolesValidos = await _context.Roles
                    .Where(r => rolesSeleccionados.Contains(r.Id_Rol) && r.Vigente == 1)
                    .Select(r => r.Id_Rol)
                    .ToListAsync();

                if (rolesValidos.Count != rolesSeleccionados.Count)
                {
                    return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Uno o mas roles seleccionados no existen o estan inactivos.");
                }
            }

            var asignacionesActuales = await _context.Roles_User
                .Where(ru => ru.Id_Usuario == idUsuario)
                .ToListAsync();

            var now = DateTime.UtcNow;

            foreach (var asignacion in asignacionesActuales)
            {
                var debeQuedarActiva = rolesSeleccionados.Contains(asignacion.Id_Rol);
                var nuevoEstado = debeQuedarActiva ? (short)1 : (short)0;

                if (asignacion.Vigente != nuevoEstado)
                {
                    asignacion.Vigente = nuevoEstado;
                    asignacion.Id_Usuario_Modifica = audit.UserId;
                    asignacion.Fecha_Modifica = now;
                    asignacion.Maquina_Modifica = audit.Machine;
                }
            }

            var rolesYaRegistrados = asignacionesActuales
                .Select(ru => ru.Id_Rol)
                .ToHashSet();

            foreach (var idRol in rolesSeleccionados.Where(idRol => !rolesYaRegistrados.Contains(idRol)))
            {
                _context.Roles_User.Add(new Roles_User
                {
                    Id_Usuario = idUsuario,
                    Id_Rol = idRol,
                    Vigente = 1,
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = now,
                    Maquina_Creacion = audit.Machine
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto asignando roles al usuario {IdUsuario}", idUsuario);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "No fue posible guardar la asignacion de roles.");
            }

            return ServiceResult.Success(
                "Roles asignados correctamente.",
                auditDescription: $"Actualizacion de roles del usuario {usuario.Nombre} con id {usuario.Id_Usuario}");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Roles;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Roles;
using System;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Services.Roles
{
    // Servicio de negocio del CRUD de roles.
    // Centraliza validacion de duplicados, auditoria y baja logica.
    public class RolesService : IRoles
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RolesService> _logger;

        public RolesService(AppDbContext context, ILogger<RolesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // P_InsRol: registra un rol nuevo validando que no exista otro con el mismo nombre.
        public async Task<ServiceResult> P_InsRol(DtoRolCreateRequest model, AuditContext audit)
        {
            var rol = model.Rol.Trim();
            var rolNormalizado = rol.ToLowerInvariant();

            var existe = await _context.Roles
                .AnyAsync(r => r.Rol.ToLower() == rolNormalizado);

            if (existe)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El rol ya existe.");
            }

            var nuevo = new Models.Administracion.Roles
            {
                Rol = rol,
                Vigente = 1,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.Roles.Add(nuevo);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto registrando rol {Rol}", rol);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El rol ya existe.");
            }

            return ServiceResult.Success(
                "Rol registrado correctamente.",
                auditDescription: $"Registro del rol {nuevo.Rol} con id {nuevo.Id_Rol}");
        }

        // F_GetRolesList: consulta todos los roles para alimentar la grilla.
        public async Task<ServiceResult> F_GetRolesList()
        {
            var roles = await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.Rol)
                .Select(r => new
                {
                    r.Id_Rol,
                    r.Rol,
                    r.Vigente,
                    r.Fecha_Creacion
                })
                .ToListAsync();

            return ServiceResult.Success(data: roles);
        }

        // F_GetRol: consulta un rol por id para cargar el modal de actualizacion.
        public async Task<ServiceResult> F_GetRol(int idRol)
        {
            var rol = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id_Rol == idRol);

            if (rol == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Rol no encontrado.");
            }

            return ServiceResult.Success(data: new
            {
                rol.Id_Rol,
                rol.Rol,
                rol.Vigente
            }, auditDescription: $"Consulta del rol {rol.Rol} con id {rol.Id_Rol}");
        }

        // P_UdpRol: actualiza el nombre y estado de un rol existente.
        public async Task<ServiceResult> P_UdpRol(int idRol, DtoRolUpdateRequest model, AuditContext audit)
        {
            var rol = await _context.Roles.FindAsync(idRol);
            if (rol == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Rol no existe.");
            }

            var nombreRol = model.Rol.Trim();
            var rolNormalizado = nombreRol.ToLowerInvariant();

            var duplicado = await _context.Roles
                .AnyAsync(r => r.Id_Rol != idRol && r.Rol.ToLower() == rolNormalizado);

            if (duplicado)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El rol ya existe.");
            }

            rol.Rol = nombreRol;
            rol.Vigente = model.Vigente;
            rol.Id_Usuario_Modifica = audit.UserId;
            rol.Fecha_Modifica = DateTime.UtcNow;
            rol.Maquina_Modifica = audit.Machine;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto actualizando rol {IdRol}", idRol);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El rol ya existe.");
            }

            return ServiceResult.Success(
                "Rol actualizado correctamente.",
                auditDescription: $"Actualizacion del rol {rol.Rol} con id {rol.Id_Rol}");
        }

        // P_DeleteRol: realiza baja logica marcando Vigente = 0.
        public async Task<ServiceResult> P_DeleteRol(int idRol, AuditContext audit)
        {
            try
            {
                var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Id_Rol == idRol);
                if (rol == null)
                {
                    return ServiceResult.Fail(StatusCodes.Status404NotFound, "El rol no existe.");
                }

                if (rol.Vigente == 0)
                {
                    return ServiceResult.Success(
                        "El rol ya se encontraba inactivo.",
                        auditDescription: $"Eliminacion logica del rol {rol.Rol} con id {rol.Id_Rol}");
                }

                rol.Vigente = 0;
                rol.Id_Usuario_Modifica = audit.UserId;
                rol.Fecha_Modifica = DateTime.UtcNow;
                rol.Maquina_Modifica = audit.Machine;

                await _context.SaveChangesAsync();

                return ServiceResult.Success(
                    "El rol fue marcado como inactivo correctamente.",
                    auditDescription: $"Eliminacion logica del rol {rol.Rol} con id {rol.Id_Rol}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando rol {IdRol}", idRol);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "Ocurrio un error al eliminar el rol.");
            }
        }
    }
}

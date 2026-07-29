using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Usuarios;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Usuarios;
using Tienda_Streaming.Services.Email;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Services.Usuarios
{
    // Servicio de negocio del CRUD de usuarios.
    // Centraliza duplicados, hashing de contrasena, auditoria y baja logica.
    public class UsuariosService : IUsuarios
    {
        private const string NombreEmpresa = "Tienda Streaming";
        private const string TipoUsuarioSistema = "Usuario del sistema";
        private readonly AppDbContext _context;
        private readonly ILogger<UsuariosService> _logger;
        private readonly IEmailSender _emailSender;

        public UsuariosService(AppDbContext context, ILogger<UsuariosService> logger, IEmailSender emailSender)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
        }

        // ExistenUsuarios: permite al controlador decidir si el registro inicial puede ser anonimo.
        public async Task<bool> ExistenUsuarios()
        {
            return await _context.Usuarios.AsNoTracking().AnyAsync();
        }

        // P_InsUsuario: registra un usuario y guarda la contrasena como hash BCrypt.
        public async Task<ServiceResult> P_InsUsuario(DtoUsuarioCreateRequest model, AuditContext audit, bool esRegistroInicial, string? linkAcceso)
        {
            var nombre = model.Nombre.Trim();
            var nombreUsuario = model.Usuario.Trim();
            var email = model.E_Mail.Trim().ToLowerInvariant();
            var usuarioNormalizado = nombreUsuario.ToLowerInvariant();
            var passwordPlano = string.IsNullOrWhiteSpace(model.Password)
                ? GenerarPasswordTemporal()
                : model.Password.Trim();

            if (esRegistroInicial && string.IsNullOrWhiteSpace(model.Password))
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "La contrasena es obligatoria para crear el primer usuario.");
            }

            var existe = await _context.Usuarios
                .AnyAsync(u => u.Usuario.ToLower() == usuarioNormalizado || u.E_Mail == email);

            if (existe)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El usuario o email ya existe");
            }

            var nuevo = new Models.Administracion.Usuarios
            {
                Nombre = nombre,
                Usuario = nombreUsuario,
                E_Mail = email,
                Password = BCrypt.Net.BCrypt.HashPassword(passwordPlano, workFactor: 12),
                Vigente = 1,
                Debe_Cambiar_Password = esRegistroInicial ? (short)0 : (short)1,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.Usuarios.Add(nuevo);

            try
            {
                await _context.SaveChangesAsync();

                if (esRegistroInicial)
                {
                    await AsignarRolSuperUsuarioInicial(nuevo, audit);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto registrando usuario {Usuario}", nombreUsuario);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El usuario o email ya existe");
            }

            var mensaje = esRegistroInicial
                ? "Primer usuario registrado correctamente"
                : "Usuario registrado correctamente. Se proceso el envio del correo de acceso al usuario.";

            var credenciales = new
            {
                Plataforma = NombreEmpresa,
                TipoUsuario = TipoUsuarioSistema,
                Nombre = nombre,
                Usuario = nombreUsuario,
                Correo = email,
                Contrasena = passwordPlano,
                LinkAcceso = linkAcceso ?? string.Empty
            };

            if (!esRegistroInicial)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(linkAcceso))
                    {
                        mensaje = "Usuario registrado correctamente, pero no fue posible generar el link de acceso.";
                    }
                    else
                    {
                        await _emailSender.SendNewUserAccessAsync(
                            email,
                            nombreUsuario,
                            passwordPlano,
                            linkAcceso,
                            NombreEmpresa,
                            TipoUsuarioSistema);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "No fue posible enviar el correo de acceso al usuario {UsuarioId}", nuevo.Id_Usuario);
                    mensaje = "Usuario registrado correctamente, pero no fue posible enviar el correo de acceso.";
                }
            }

            return ServiceResult.Success(
                mensaje,
                data: new { credenciales },
                auditDescription: $"Registro del usuario {nuevo.Usuario} con id {nuevo.Id_Usuario}");
        }

        // F_GetUsuariosList: consulta usuarios para alimentar la grilla principal.
        public async Task<ServiceResult> F_GetUsuariosList()
        {
            var usuarios = await _context.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Nombre)
                .Select(u => new
                {
                    u.Id_Usuario,
                    u.Nombre,
                    u.Usuario,
                    u.E_Mail,
                    u.Vigente,
                    u.Debe_Cambiar_Password,
                    u.Fecha_Creacion
                })
                .ToListAsync();

            return ServiceResult.Success(data: usuarios);
        }

        // F_GetUsuario: consulta un usuario por id para cargar el modal de actualizacion.
        public async Task<ServiceResult> F_GetUsuario(int idUsuario)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id_Usuario == idUsuario);

            if (usuario == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Usuario no encontrado");
            }

            return ServiceResult.Success(data: new
            {
                usuario.Id_Usuario,
                usuario.Nombre,
                usuario.Usuario,
                usuario.E_Mail,
                usuario.Vigente,
                usuario.Debe_Cambiar_Password
            }, auditDescription: $"Consulta del usuario {usuario.Usuario} con id {usuario.Id_Usuario}");
        }

        // P_UdpUsuario: actualiza datos generales y estado vigente de un usuario.
        public async Task<ServiceResult> P_UdpUsuario(int idUsuario, DtoUsuarioUpdateRequest model, AuditContext audit)
        {
            var usuario = await _context.Usuarios.FindAsync(idUsuario);
            if (usuario == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Usuario no existe");
            }

            if (audit.UserId == idUsuario && model.Vigente == 0)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "No puedes desactivar tu propio usuario.");
            }

            var nombreUsuario = model.Usuario.Trim();
            var email = model.E_Mail.Trim().ToLowerInvariant();
            var usuarioNormalizado = nombreUsuario.ToLowerInvariant();

            var duplicado = await _context.Usuarios
                .AnyAsync(x => x.Id_Usuario != idUsuario &&
                    (x.Usuario.ToLower() == usuarioNormalizado || x.E_Mail == email));

            if (duplicado)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El usuario o email ya existe");
            }

            usuario.Nombre = model.Nombre.Trim();
            usuario.Usuario = nombreUsuario;
            usuario.E_Mail = email;
            usuario.Vigente = model.Vigente;
            usuario.Id_Usuario_Modifica = audit.UserId;
            usuario.Fecha_Modifica = DateTime.UtcNow;
            usuario.Maquina_Modifica = audit.Machine;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflicto actualizando usuario {IdUsuario}", idUsuario);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "El usuario o email ya existe");
            }

            return ServiceResult.Success(
                "Usuario actualizado correctamente",
                auditDescription: $"Actualizacion del usuario {usuario.Usuario} con id {usuario.Id_Usuario}");
        }

        // P_DeleteUsuario: realiza baja logica y evita que el usuario actual se elimine a si mismo.
        public async Task<ServiceResult> P_DeleteUsuario(int idUsuario, AuditContext audit)
        {
            try
            {
                if (audit.UserId == idUsuario)
                {
                    return ServiceResult.Fail(StatusCodes.Status400BadRequest, "No puedes eliminar tu propio usuario.");
                }

                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario);
                if (usuario == null)
                {
                    return ServiceResult.Fail(StatusCodes.Status404NotFound, "El usuario no existe.");
                }

                if (usuario.Vigente == 0)
                {
                    return ServiceResult.Success(
                        "El usuario ya se encontraba inactivo.",
                        auditDescription: $"Eliminacion logica del usuario {usuario.Usuario} con id {usuario.Id_Usuario}");
                }

                usuario.Vigente = 0;
                usuario.Id_Usuario_Modifica = audit.UserId;
                usuario.Fecha_Modifica = DateTime.UtcNow;
                usuario.Maquina_Modifica = audit.Machine;

                await _context.SaveChangesAsync();

                return ServiceResult.Success(
                    "El usuario fue marcado como inactivo correctamente.",
                    auditDescription: $"Eliminacion logica del usuario {usuario.Usuario} con id {usuario.Id_Usuario}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario {IdUsuario}", idUsuario);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "Ocurrio un error al eliminar el usuario.");
            }
        }

        // Al crear el primer usuario se garantiza y asigna el rol 1, reservado como super usuario.
        private async Task AsignarRolSuperUsuarioInicial(Models.Administracion.Usuarios usuario, AuditContext audit)
        {
            var rolSuperUsuario = await _context.Roles
                .FirstOrDefaultAsync(r => r.Id_Rol == 1);

            if (rolSuperUsuario == null)
            {
                var existeRolAdministrador = await _context.Roles
                    .AsNoTracking()
                    .AnyAsync(r => r.Rol.ToLower() == "administrador");

                rolSuperUsuario = new Models.Administracion.Roles
                {
                    Id_Rol = 1,
                    Rol = existeRolAdministrador ? "Super Usuario" : "Administrador",
                    Vigente = 1,
                    Id_Usuario_Creacion = usuario.Id_Usuario,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                };

                _context.Roles.Add(rolSuperUsuario);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync("""
                    SELECT setval(
                        pg_get_serial_sequence('"Roles"', 'Id_Rol'),
                        COALESCE((SELECT MAX("Id_Rol") FROM "Roles"), 1),
                        true
                    );
                    """);
            }
            else if (rolSuperUsuario.Vigente == 0)
            {
                rolSuperUsuario.Vigente = 1;
                rolSuperUsuario.Id_Usuario_Modifica = usuario.Id_Usuario;
                rolSuperUsuario.Fecha_Modifica = DateTime.UtcNow;
                rolSuperUsuario.Maquina_Modifica = audit.Machine;
                await _context.SaveChangesAsync();
            }

            var yaTieneRol = await _context.Roles_User
                .AsNoTracking()
                .AnyAsync(ru => ru.Id_Usuario == usuario.Id_Usuario && ru.Id_Rol == 1);

            if (yaTieneRol)
            {
                return;
            }

            _context.Roles_User.Add(new Roles_User
            {
                Id_Usuario = usuario.Id_Usuario,
                Id_Rol = 1,
                Vigente = 1,
                Id_Usuario_Creacion = audit.UserId ?? usuario.Id_Usuario,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            });

            await _context.SaveChangesAsync();
        }

        // Genera una contrasena temporal compatible con la politica actual:
        // minimo 10 caracteres, mayuscula, minuscula y numero.
        private static string GenerarPasswordTemporal()
        {
            const string minusculas = "abcdefghijkmnopqrstuvwxyz";
            const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string numeros = "23456789";
            const string simbolos = "!@$%*?";
            const string todos = minusculas + mayusculas + numeros + simbolos;

            var caracteres = new[]
            {
                minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)],
                mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)],
                numeros[RandomNumberGenerator.GetInt32(numeros.Length)],
                simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)]
            }.ToList();

            while (caracteres.Count < 14)
            {
                caracteres.Add(todos[RandomNumberGenerator.GetInt32(todos.Length)]);
            }

            return new string(caracteres
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray());
        }

    }
}

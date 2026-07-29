using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad que representa la tabla Usuarios.
    // Se usa para login, recuperacion de contrasena y CRUD de usuarios.
    public class Usuarios
    {
        [Key]
        public int Id_Usuario { get; set; }

        // Nombre visible de la persona.
        [Required]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        // Nombre de usuario para login. Debe ser unico por configuracion del DbContext.
        [Required]
        [StringLength(60)]
        public string Usuario { get; set; } = string.Empty;

        // Correo usado tambien para login y recuperacion de contrasena.
        [StringLength(160)]
        public string? E_Mail { get; set; }

        // Hash BCrypt de la contrasena. Nunca debe almacenarse texto plano.
        [Required]
        [StringLength(100)]
        public string Password { get; set; } = string.Empty;

        // Baja logica: 1 activo, 0 inactivo.
        public short Vigente { get; set; } = 1;

        // Obliga al usuario a cambiar la contrasena despues de iniciar sesion.
        // Se activa para usuarios creados por administrador con clave temporal.
        public short Debe_Cambiar_Password { get; set; } = 0;

        // Campos de auditoria de creacion.
        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        // Campos de auditoria de ultima modificacion.
        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }
    }

}

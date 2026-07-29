using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad de roles. Un rol agrupa permisos y puede asignarse a usuarios.
    public class Roles
    {
        [Key]
        public int Id_Rol { get; set; }

        // Nombre del rol, por ejemplo Administrador.
        [Required]
        [StringLength(80)]
        public string Rol { get; set; } = string.Empty;

        // Baja logica: 1 activo, 0 inactivo.
        public short Vigente { get; set; } = 1;

        // Auditoria de creacion.
        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        // Auditoria de modificacion.
        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }
    }

}

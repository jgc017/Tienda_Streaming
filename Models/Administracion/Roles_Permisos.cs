using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad puente entre Roles y Permisos.
    // Permite que un rol tenga muchas acciones autorizadas.
    public class Roles_Permisos
    {
        [Key]
        public int Id_Rol_Permiso { get; set; }

        // Llaves foraneas configuradas en AppDbContext.
        public int Id_Rol { get; set; }
        public int Id_Permiso { get; set; }

        // Baja logica de la asignacion rol-permiso.
        public short Vigente { get; set; } = 1;

        // Auditoria de creacion.
        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        // Auditoria de modificacion.
        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        // Propiedades de navegacion para traer rol y permiso relacionados.
        public Roles Rol { get; set; } = null!;
        public Permisos Permiso { get; set; } = null!;
    }

}

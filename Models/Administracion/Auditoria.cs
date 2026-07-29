using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad para registrar eventos administrativos relevantes del sistema.
    public class Auditoria
    {
        [Key]
        public int Id_Auditoria { get; set; }

        // Descripcion del evento auditado.
        [StringLength(500)]
        public string? Descripcion { get; set; }

        // Formulario desde donde se genero el evento, por ejemplo VwDominios.
        [StringLength(120)]
        public string? Formulario { get; set; }

        // Metodo ejecutado dentro del flujo. Para ingreso a formulario se usa N/A.
        [StringLength(120)]
        public string? Metodo_Ejecutado { get; set; }

        // Baja logica del registro de auditoria.
        public short Vigente { get; set; } = 1;

        // Usuario y fecha de creacion del evento.
        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        // Usuario y fecha de modificacion cuando aplique.
        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        // Navegaciones a los usuarios relacionados con la auditoria.
        public Usuarios? UsuarioCrea { get; set; }
        public Usuarios? UsuarioModifica { get; set; }
    }

}

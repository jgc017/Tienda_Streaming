using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad de dominios/catalogos. Soporta jerarquia mediante Id_Padre.
    public class Dominios
    {
        [Key]
        public int Id_Dominio { get; set; }

        // Texto visible del item de catalogo.
        [Required]
        [StringLength(120)]
        public string Descripcion { get; set; } = string.Empty;

        // Dominio padre opcional para arboles o listas dependientes.
        public int? Id_Padre { get; set; }

        // Indica si este dominio puede recibir subdominios hijos. Valores permitidos: Si, No.
        [Required]
        [StringLength(2)]
        public string DominioPadre { get; set; } = "No";

        // Baja logica: 1 activo, 0 inactivo.
        public short Vigente { get; set; } = 1;

        // Auditoria de creacion.
        public int? Id_Usuario_Crea { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        // Auditoria de modificacion.
        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        // Navegacion al dominio padre.
        public Dominios? Padre { get; set; }
    }

}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class PreciosProducto
    {
        [Key]
        public int Id_Precio_Producto { get; set; }

        public int Id_Plataforma { get; set; }

        public int Id_Tipo_Usuario { get; set; }

        public int Tiempo_Pantalla { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Precio { get; set; }

        public short Vigente { get; set; } = 1;

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        public Dominios? Plataforma { get; set; }
        public Dominios? TipoUsuario { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class CodigosCompra
    {
        [Key]
        public int Id_Codigo_Compra { get; set; }

        [Required]
        [StringLength(40)]
        public string Codigo { get; set; } = string.Empty;

        [Column(TypeName = "numeric(12,2)")]
        public decimal Valor_Inicial { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Saldo_Disponible { get; set; }

        [Required]
        [StringLength(120)]
        public string Nombre_Cliente { get; set; } = string.Empty;

        [Required]
        [StringLength(160)]
        public string Correo_Cliente { get; set; } = string.Empty;

        public DateTime? Fecha_Expiracion { get; set; }

        public short Vigente { get; set; } = 1;

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }
    }
}

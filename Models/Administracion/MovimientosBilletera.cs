using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class MovimientosBilletera
    {
        [Key]
        public int Id_Movimiento_Billetera { get; set; }

        public int Id_Billetera { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo_Movimiento { get; set; } = string.Empty;

        [Column(TypeName = "numeric(12,2)")]
        public decimal Valor { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Saldo_Anterior { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Saldo_Nuevo { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public int? Id_Pedido { get; set; }

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public BilleteraVendedores? Billetera { get; set; }
        public Pedidos? Pedido { get; set; }
    }
}

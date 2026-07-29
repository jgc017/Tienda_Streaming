using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class PedidoDetalles
    {
        [Key]
        public int Id_Pedido_Detalle { get; set; }

        public int Id_Pedido { get; set; }

        [Required]
        [StringLength(20)]
        public string Tipo_Producto { get; set; } = string.Empty;

        public int? Id_Plataforma { get; set; }

        public int? Id_Combo { get; set; }

        public int Tiempo_Pantalla { get; set; }

        public int Cantidad { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Precio_Unitario { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Subtotal { get; set; }

        public Pedidos? Pedido { get; set; }
        public Dominios? Plataforma { get; set; }
        public Combos? Combo { get; set; }
    }
}

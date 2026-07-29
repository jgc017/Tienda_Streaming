using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class Pedidos
    {
        [Key]
        public int Id_Pedido { get; set; }

        [Required]
        [StringLength(20)]
        public string Origen { get; set; } = string.Empty;

        public int Id_Tipo_Usuario { get; set; }

        public int? Id_Usuario { get; set; }

        public int? Id_Codigo_Compra { get; set; }

        [Required]
        [StringLength(120)]
        public string Nombre_Cliente { get; set; } = string.Empty;

        [StringLength(160)]
        public string? Correo_Cliente { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Total { get; set; }

        public DateTime Fecha_Compra { get; set; }

        public short Vigente { get; set; } = 1;

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public Dominios? TipoUsuario { get; set; }
        public Usuarios? Usuario { get; set; }
        public CodigosCompra? CodigoCompra { get; set; }
        public ICollection<PedidoDetalles> Detalles { get; set; } = new List<PedidoDetalles>();
        public ICollection<PedidoCuentas> Cuentas { get; set; } = new List<PedidoCuentas>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    public class PedidoCuentas
    {
        [Key]
        public int Id_Pedido_Cuenta { get; set; }

        public int Id_Pedido { get; set; }

        public int Id_Cuenta { get; set; }

        public int? Id_Pedido_Detalle { get; set; }

        public Pedidos? Pedido { get; set; }
        public Cuentas? Cuenta { get; set; }
        public PedidoDetalles? PedidoDetalle { get; set; }
    }
}

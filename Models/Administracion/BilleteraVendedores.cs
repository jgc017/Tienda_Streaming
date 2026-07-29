using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class BilleteraVendedores
    {
        [Key]
        public int Id_Billetera { get; set; }

        public int Id_Usuario { get; set; }

        [Column(TypeName = "numeric(12,2)")]
        public decimal Saldo { get; set; }

        public short Vigente { get; set; } = 1;

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        public Usuarios? Usuario { get; set; }
    }
}

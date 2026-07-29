using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    public class ComboPlataformas
    {
        [Key]
        public int Id_Combo_Plataforma { get; set; }

        public int Id_Combo { get; set; }

        public int Id_Plataforma { get; set; }

        public int Cantidad { get; set; } = 1;

        public Combos? Combo { get; set; }
        public Dominios? Plataforma { get; set; }
    }
}

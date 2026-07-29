namespace Tienda_Streaming.Models.Dto.General
{
    // DTO ligero para cargar selects/dropdowns desde la tabla Dominios.
    public class DtoDominioDropdownItem
    {
        public int Id_Dominio { get; set; }

        public string Descripcion { get; set; } = string.Empty;

    }
}

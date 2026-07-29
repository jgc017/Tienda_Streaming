namespace Tienda_Streaming.Models.Dto.General
{
    // DTO ligero para cargar selects/dropdowns desde la tabla Menus.
    public class DtoMenuDropdownItem
    {
        public int Id_Menu { get; set; }

        public string Descripcion { get; set; } = string.Empty;
    }
}

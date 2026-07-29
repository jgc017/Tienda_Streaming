using System.Collections.Generic;

namespace Tienda_Streaming.Models.Dto.General
{
    // DTO usado por el layout para pintar el menu lateral dinamico.
    public class DtoMenuSistemaItem
    {
        public int Id_Menu { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int? Id_Padre { get; set; }
        public int Posicion { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Controlador { get; set; }
        public string? Vista { get; set; }
        public string Icono { get; set; } = "fa-solid fa-circle";
        public int Nivel { get; set; }
        public bool TieneRuta => !string.IsNullOrWhiteSpace(Controlador) && !string.IsNullOrWhiteSpace(Vista);
        public List<DtoMenuSistemaItem> Hijos { get; set; } = new();
    }
}

using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.Dominios
{
    // DTO recibido por POST /api/DominiosApi/P_InsDominio.
    // Representa un nuevo dominio hijo enviado desde la vista.
    public class DtoDominioCreateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El dominio padre es obligatorio")]
        public int Id_Padre { get; set; }

        [Required(ErrorMessage = "La descripcion es obligatoria")]
        [StringLength(120, MinimumLength = 2)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Si|No)$", ErrorMessage = "Debe Indicar si es o no un dominio padre")]
        public string DominioPadre { get; set; } = "No";

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    // DTO recibido por PUT /api/DominiosApi/P_UdpDominio/{id}.
    // Representa los datos editables de un dominio existente.
    public class DtoDominioUpdateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El dominio padre es obligatorio")]
        public int Id_Padre { get; set; }

        [Required(ErrorMessage = "La descripcion es obligatoria")]
        [StringLength(120, MinimumLength = 2)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Si|No)$", ErrorMessage = "DominioPadre debe ser Si o No")]
        public string DominioPadre { get; set; } = "No";

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }
}

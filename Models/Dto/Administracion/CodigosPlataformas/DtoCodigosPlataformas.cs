using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.CodigosPlataformas
{
    public class DtoBuscarCorreoPlataformaRequest
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
        [StringLength(160)]
        public string Correo { get; set; } = string.Empty;
    }

    public class DtoDetalleCorreoPlataformaRequest : DtoBuscarCorreoPlataformaRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El correo recibido es obligatorio")]
        public int Id_Correo_Plataforma { get; set; }
    }

    public class DtoCorreoPlataformaItem
    {
        public int Id_Correo_Plataforma { get; set; }
        public string Remitente { get; set; } = string.Empty;
        public string Destinatarios { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public DateTime Fecha_Recepcion { get; set; }
        public DateTime Fecha_Registro { get; set; }
    }

    public class DtoCorreoPlataformaDetalle : DtoCorreoPlataformaItem
    {
        public string Cuerpo_Texto { get; set; } = string.Empty;
        public string Cuerpo_Html { get; set; } = string.Empty;
        public List<DtoCorreoPlataformaEnlace> Enlaces { get; set; } = new();
    }

    public class DtoCorreoPlataformaEnlace
    {
        public string Texto { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}

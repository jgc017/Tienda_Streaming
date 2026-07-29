using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    public class CorreosPlataforma
    {
        [Key]
        public int Id_Correo_Plataforma { get; set; }

        [StringLength(160)]
        public string? MessageId { get; set; }

        [Required]
        [StringLength(128)]
        public string Hash_Mensaje { get; set; } = string.Empty;

        [StringLength(300)]
        public string Remitente { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Destinatarios { get; set; } = string.Empty;

        [StringLength(300)]
        public string Asunto { get; set; } = string.Empty;

        public string? Encabezados { get; set; }
        public string? Cuerpo_Texto { get; set; }
        public string? Cuerpo_Html { get; set; }
        public string Texto_Busqueda { get; set; } = string.Empty;
        public DateTime Fecha_Recepcion { get; set; }
        public DateTime Fecha_Registro { get; set; }

        public int? Id_Usuario_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }
    }
}

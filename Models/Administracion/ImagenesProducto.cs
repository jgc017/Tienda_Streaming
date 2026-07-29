using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    public class ImagenesProducto
    {
        [Key]
        public int Id_ImagenProducto { get; set; }

        public int Id_Plataforma { get; set; }

        public int? Id_Tipo_Imagen { get; set; }

        public int Orden { get; set; } = 1;

        [Required]
        [StringLength(500)]
        public string ImagenUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public short Vigente { get; set; } = 1;

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        public Dominios? Plataforma { get; set; }
        public Dominios? TipoImagen { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.ImagenesProducto
{
    public class DtoImagenProductoCreateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La plataforma es obligatoria")]
        public int Id_Plataforma { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El tipo de imagen es obligatorio")]
        public int Id_Tipo_Imagen { get; set; }

        [Required(ErrorMessage = "La imagen es obligatoria")]
        [StringLength(500)]
        public string ImagenUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    public class DtoImagenProductoUpdateRequest : DtoImagenProductoCreateRequest
    {
    }

    public class DtoImagenProductoOrdenRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La imagen es obligatoria")]
        public int Id_ImagenProducto { get; set; }

        [Range(-1, 1, ErrorMessage = "La direccion del movimiento no es valida")]
        public int Direccion { get; set; }
    }

    public class DtoImagenProductoItem
    {
        public int Id_ImagenProducto { get; set; }
        public int Id_Plataforma { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public int? Id_Tipo_Imagen { get; set; }
        public string TipoImagen { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public short Vigente { get; set; }
        public DateTime Fecha_Creacion { get; set; }
    }
}

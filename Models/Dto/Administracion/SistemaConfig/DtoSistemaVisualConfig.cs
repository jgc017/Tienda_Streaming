using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.SistemaConfig
{
    // DTO de lectura para pintar la configuracion visual actual del sistema.
    public class DtoSistemaVisualConfigItem
    {
        public int Id_SistemaVisualConfig { get; set; }
        public string NombreSistema { get; set; } = "Tienda Streaming";
        public string LogoUrl { get; set; } = "/uploads/sistema/logo_20260801014229_436a9b3c1af244fca0be8dd75454e495.png";
        public string FaviconUrl { get; set; } = "/favicon.ico";
        public string LoginBackgroundUrl { get; set; } = "/uploads/sistema/loginbackground_20260801014351_1b013f02f27345b4ac09677148c3ffe3.png";
        public string? VideoUrl { get; set; }
    }

    // DTO de actualizacion. Recibe las rutas publicas previamente cargadas en wwwroot.
    public class DtoSistemaVisualConfigUpdateRequest
    {
        [Required(ErrorMessage = "El nombre del sistema es obligatorio.")]
        [StringLength(120, MinimumLength = 2, ErrorMessage = "El nombre del sistema debe tener entre 2 y 120 caracteres.")]
        public string NombreSistema { get; set; } = string.Empty;
        [Required(ErrorMessage = "La ruta del logo es obligatoria.")]
        [StringLength(500, ErrorMessage = "La ruta del logo no puede superar 500 caracteres.")]
        public string LogoUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ruta del favicon es obligatoria.")]
        [StringLength(500, ErrorMessage = "La ruta del favicon no puede superar 500 caracteres.")]
        public string FaviconUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ruta del fondo del login es obligatoria.")]
        [StringLength(500, ErrorMessage = "La ruta del fondo del login no puede superar 500 caracteres.")]
        public string LoginBackgroundUrl { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "La ruta o referencia del video no puede superar 500 caracteres.")]
        public string? VideoUrl { get; set; }
    }
}

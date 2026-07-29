using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad SistemaVisualConfig: guarda las rutas visuales globales del sistema.
    // Se usa para cambiar logo, favicon, fondo del login y video publico sin modificar codigo.
    public class SistemaVisualConfig
    {
        [Key]
        public int Id_SistemaVisualConfig { get; set; }

        // Logo principal usado en login, loader, inicio publico y menu interno.
        [Required]
        [StringLength(500)]
        public string LogoUrl { get; set; } = "/img/IMAGENIA.png";

        // Icono del navegador. Debe apuntar a un archivo local de wwwroot.
        [Required]
        [StringLength(500)]
        public string FaviconUrl { get; set; } = "/favicon.ico";

        // Imagen de fondo del login.
        [Required]
        [StringLength(500)]
        public string LoginBackgroundUrl { get; set; } = "/img/auth-background.svg";

        // Video destacado de las tiendas publicas. Acepta archivo local o enlace de YouTube.
        [StringLength(500)]
        public string? VideoUrl { get; set; }

        // Baja logica: 1 activo, 0 inactivo.
        public short Vigente { get; set; } = 1;

        // Auditoria de creacion.
        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        // Auditoria de modificacion.
        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }
    }
}

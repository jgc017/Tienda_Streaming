using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad que centraliza el contenido editable del inicio publico.
    // TipoContenido define si el registro pertenece al slider, noticias,
    // publicaciones, acerca de nosotros, contacto o politicas de privacidad.
    public class InicioContenido
    {
        [Key]
        public int Id_InicioContenido { get; set; }

        [Required]
        [StringLength(40)]
        public string TipoContenido { get; set; } = string.Empty;

        [Required]
        [StringLength(160)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Resumen { get; set; }

        public string? Contenido { get; set; }

        [StringLength(500)]
        public string? ImagenUrl { get; set; }

        [StringLength(500)]
        public string? EnlaceUrl { get; set; }

        [StringLength(80)]
        public string? TextoBoton { get; set; }

        // Indica si el registro debe mostrarse en los bloques resumidos del inicio.
        public short MostrarEnInicio { get; set; } = 1;

        // Orden visual en carruseles, listas y bloques.
        public int Orden { get; set; }

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


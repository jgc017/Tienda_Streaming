using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad de permisos. Define acciones autorizables por modulo.
    public class Permisos
    {
        [Key]
        public int Id_Permiso { get; set; }

        // Menu al que aplica el permiso cuando la accion es de visualizacion.
        // Para permisos de metodos futuros puede quedar null.
        public int? Id_Menu { get; set; }

        // Tipo funcional del permiso: Menu o Metodo.
        [Required]
        [StringLength(20)]
        public string TipoPermiso { get; set; } = "Menu";

        // Modulo funcional, por ejemplo Usuarios.
        [Required]
        [StringLength(80)]
        public string Modulo { get; set; } = string.Empty;

        // Accion dentro del modulo, por ejemplo Crear, Editar o Eliminar.
        [Required]
        [StringLength(80)]
        public string Accion { get; set; } = string.Empty;

        // Descripcion legible del permiso.
        [StringLength(200)]
        public string? Descripcion { get; set; }

        // Metadata usada por permisos automaticos de metodos API.
        [StringLength(120)]
        public string? Controlador { get; set; }

        [StringLength(120)]
        public string? Metodo { get; set; }

        [StringLength(20)]
        public string? HttpMetodo { get; set; }

        [StringLength(300)]
        public string? CodigoPermiso { get; set; }

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

        // Propiedad de navegacion para consultar el menu asociado.
        public Menus? Menu { get; set; }
    }

}

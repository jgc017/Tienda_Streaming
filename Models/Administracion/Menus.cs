using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Administracion
{
    // Entidad Menus: almacena las opciones visibles del menu principal.
    // Permite construir menus dinamicos y asociar permisos de visualizacion.
    public class Menus
    {
        [Key]
        public int Id_Menu { get; set; }

        // Texto que se muestra al usuario dentro del menu.
        [Required]
        [StringLength(255)]
        public string Descripcion { get; set; } = string.Empty;

        // Relacion jerarquica. Null indica menu padre o menu raiz.
        public int? Id_Padre { get; set; }

        // Orden visual dentro del mismo nivel jerarquico.
        public int Posicion { get; set; }

        // Tipo funcional, por ejemplo Padre, Hijo o Menu.
        [StringLength(255)]
        public string? Tipo { get; set; }

        // Controlador MVC al que apunta el menu.
        [StringLength(255)]
        public string? Controlador { get; set; }

        // Vista o accion MVC al que apunta el menu.
        [StringLength(255)]
        public string? Vista { get; set; }

        // Clase CSS del icono que se renderiza en el layout.
        [StringLength(255)]
        public string? Icono { get; set; }

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

        // Navegacion EF hacia el menu padre.
        public Menus? Padre { get; set; }
    }
}

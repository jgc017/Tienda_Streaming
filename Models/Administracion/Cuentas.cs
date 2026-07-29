using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tienda_Streaming.Models.Administracion
{
    public class Cuentas
    {
        [Key]
        public int Id_Cuenta { get; set; }

        public int Id_Plataforma { get; set; }

        public int Id_Tipo_Usuario { get; set; }

        public int Tiempo_Pantalla { get; set; } = 30;

        [Required]
        [StringLength(160)]
        public string Correo_Cuenta { get; set; } = string.Empty;

        [Column("ContraseÃ±a_Cuenta")]
        [Required]
        [StringLength(1000)]
        public string Contrasena_Cuenta { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Perfil_Cuenta { get; set; }

        [StringLength(20)]
        public string? Pin_Cuenta { get; set; }

        public DateTime? Fecha_Vencimiento { get; set; }

        public short Vigente { get; set; } = 1;

        public int? Id_Usuario_Creacion { get; set; }
        public DateTime Fecha_Creacion { get; set; }
        public string? Maquina_Creacion { get; set; }

        public int? Id_Usuario_Modifica { get; set; }
        public DateTime? Fecha_Modifica { get; set; }
        public string? Maquina_Modifica { get; set; }

        public Dominios? Plataforma { get; set; }
        public Dominios? TipoUsuario { get; set; }
    }
}


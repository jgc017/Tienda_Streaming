using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.Usuarios
{
    // DTO recibido por POST /api/UsuariosApi/P_InsUsuario.
    // Contiene contrasena porque se usa solo al crear usuarios.
    public class DtoUsuarioCreateRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El usuario es obligatorio")]
        [StringLength(60, MinimumLength = 3)]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        [StringLength(160)]
        public string E_Mail { get; set; } = string.Empty;

        [StringLength(128, MinimumLength = 10)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contrasena debe incluir mayuscula, minuscula y numero")]
        public string? Password { get; set; }
    }

    // DTO recibido por PUT /api/UsuariosApi/P_UdpUsuario/{id}.
    // No incluye contrasena para evitar cambios accidentales desde el modal de edicion.
    public class DtoUsuarioUpdateRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El usuario es obligatorio")]
        [StringLength(60, MinimumLength = 3)]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        [StringLength(160)]
        public string E_Mail { get; set; } = string.Empty;

        // Estado administrable desde el toggle de VwUsuarios.
        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }
}

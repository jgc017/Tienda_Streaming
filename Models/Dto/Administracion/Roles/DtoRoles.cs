using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.Roles
{
    // DTO recibido por POST /api/RolesApi/P_InsRol.
    // Solo contiene los datos que la vista necesita para crear un rol.
    public class DtoRolCreateRequest
    {
        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(80, MinimumLength = 2)]
        public string Rol { get; set; } = string.Empty;
    }

    // DTO recibido por PUT /api/RolesApi/P_UdpRol/{id}.
    // Incluye Vigente para permitir activar/inactivar desde el modal.
    public class DtoRolUpdateRequest
    {
        [Required(ErrorMessage = "El rol es obligatorio")]
        [StringLength(80, MinimumLength = 2)]
        public string Rol { get; set; } = string.Empty;

        // Estado administrable desde el toggle de la vista Roles.
        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }
}

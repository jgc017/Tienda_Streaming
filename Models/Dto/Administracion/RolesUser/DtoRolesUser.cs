using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.RolesUser
{
    // DTO recibido por PUT /api/RolesUserApi/asignar/{id_Usuario}.
    // Contiene los roles que deben quedar activos para el usuario indicado.
    public class DtoRolesUserUpdateRequest
    {
        [Required]
        public List<int> RoleIds { get; set; } = [];
    }
}

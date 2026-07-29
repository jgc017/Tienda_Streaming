using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Tienda_Streaming.Models.Dto.Administracion.Permisos
{
    // DTO recibido por POST /api/PermisosApi/P_InsPermiso.
    public class DtoPermisoCreateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El menu es obligatorio")]
        public int? Id_Menu { get; set; }

        [Required(ErrorMessage = "El modulo es obligatorio")]
        [StringLength(80, MinimumLength = 2)]
        public string Modulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La accion es obligatoria")]
        [StringLength(80, MinimumLength = 2)]
        public string Accion { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }
    }

    // DTO recibido por PUT /api/PermisosApi/P_UdpPermiso/{id}.
    public class DtoPermisoUpdateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El menu es obligatorio")]
        public int? Id_Menu { get; set; }

        [Required(ErrorMessage = "El modulo es obligatorio")]
        [StringLength(80, MinimumLength = 2)]
        public string Modulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La accion es obligatoria")]
        [StringLength(80, MinimumLength = 2)]
        public string Accion { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    // DTO recibido por POST /api/PermisosApi/P_InsPermisoRol/{id}.
    // Crea o reactiva la asignacion de un permiso a un rol.
    public class DtoPermisoRolCreateRequest
    {
        [Range(2, int.MaxValue, ErrorMessage = "Selecciona un rol valido")]
        public int Id_Rol { get; set; }
    }

    // DTO recibido por PUT /api/PermisosApi/P_UdpRolesPermiso/{id}.
    // Envia todos los roles asignables con el estado que debe quedar para el permiso.
    public class DtoPermisoRolBulkUpdateRequest
    {
        [Required(ErrorMessage = "La lista de roles es obligatoria")]
        public List<DtoPermisoRolBulkItem> Roles { get; set; } = new();
    }

    // Item usado por el guardado masivo de roles por permiso.
    public class DtoPermisoRolBulkItem
    {
        [Range(2, int.MaxValue, ErrorMessage = "Selecciona un rol valido")]
        public int Id_Rol { get; set; }

        [Range(0, 1, ErrorMessage = "El estado del rol es invalido")]
        public short Vigente { get; set; }
    }

    // DTO ligero para cargar el dropdown de roles asignables.
    public class DtoRolPermisoDropdownItem
    {
        public int Id_Rol { get; set; }
        public string Rol { get; set; } = string.Empty;
    }

    // DTO usado por GET /api/PermisosApi/F_GetRolesPorPermiso/{id}.
    // Permite pintar todos los roles con su switch activo/inactivo en una sola consulta.
    public class DtoRolPorPermisoItem
    {
        public int Id_Rol { get; set; }
        public string Rol { get; set; } = string.Empty;
        public int? Id_Rol_Permiso { get; set; }
        public short Vigente { get; set; }
        public bool Asignado { get; set; }
    }
}

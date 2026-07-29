using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.Permisos;

namespace Tienda_Streaming.Business.Interfaces.Permisos
{
    // Contrato de negocio para el CRUD de permisos.
    public interface IPermisos
    {
        Task<ServiceResult> P_InsPermiso(DtoPermisoCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetPermisosList();
        Task<ServiceResult> F_GetPermiso(int idPermiso);
        Task<ServiceResult> P_UdpPermiso(int idPermiso, DtoPermisoUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeletePermiso(int idPermiso, AuditContext audit);
        Task<List<DtoRolPermisoDropdownItem>> F_GetRolesAsignables();
        Task<ServiceResult> F_GetRolesPorPermiso(int idPermiso);
        Task<ServiceResult> P_UdpRolesPermiso(int idPermiso, DtoPermisoRolBulkUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_InsPermisoRol(int idPermiso, DtoPermisoRolCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetPermisoRol(int idPermiso, int idRol);
        Task<ServiceResult> P_DeletePermisoRol(int idPermiso, int idRol, AuditContext audit);
    }
}

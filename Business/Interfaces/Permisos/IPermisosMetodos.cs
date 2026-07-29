using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.Permisos;

namespace Tienda_Streaming.Business.Interfaces.Permisos
{
    // Contrato de negocio para permisos automaticos de metodos API.
    public interface IPermisosMetodos
    {
        Task<ServiceResult> F_GetPermisosMetodosList();
        Task<ServiceResult> F_GetPermisoMetodo(int idPermiso);
        Task<ServiceResult> P_SyncPermisosMetodos(AuditContext audit);
        Task<ServiceResult> P_UdpPermisoMetodo(int idPermiso, DtoPermisoMetodoUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeletePermisoMetodo(int idPermiso, AuditContext audit);
    }
}

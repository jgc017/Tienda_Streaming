using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.Roles;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Interfaces.Roles
{
    // Contrato de negocio para el CRUD de roles.
    public interface IRoles
    {
        Task<ServiceResult> P_InsRol(DtoRolCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetRolesList();
        Task<ServiceResult> F_GetRol(int idRol);
        Task<ServiceResult> P_UdpRol(int idRol, DtoRolUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteRol(int idRol, AuditContext audit);
    }
}

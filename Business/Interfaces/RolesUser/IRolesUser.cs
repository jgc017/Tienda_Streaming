using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.RolesUser;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Interfaces.RolesUser
{
    // Contrato de negocio para consultar y sincronizar roles asignados a usuarios.
    public interface IRolesUser
    {
        Task<ServiceResult> GetIdUserRoles(int idUsuario);
        Task<ServiceResult> Asignar(int idUsuario, DtoRolesUserUpdateRequest model, AuditContext audit);
    }
}

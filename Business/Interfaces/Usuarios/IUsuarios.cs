using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.Usuarios;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Interfaces.Usuarios
{
    // Contrato de negocio para el CRUD de usuarios.
    public interface IUsuarios
    {
        Task<bool> ExistenUsuarios();
        Task<ServiceResult> P_InsUsuario(DtoUsuarioCreateRequest model, AuditContext audit, bool esRegistroInicial, string? linkAcceso);
        Task<ServiceResult> F_GetUsuariosList();
        Task<ServiceResult> F_GetUsuario(int idUsuario);
        Task<ServiceResult> P_UdpUsuario(int idUsuario, DtoUsuarioUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteUsuario(int idUsuario, AuditContext audit);
    }
}

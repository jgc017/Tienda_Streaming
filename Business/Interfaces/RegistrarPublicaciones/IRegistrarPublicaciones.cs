using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarPublicaciones;

namespace Tienda_Streaming.Business.Interfaces.RegistrarPublicaciones
{
    // Contrato de negocio para administrar y consultar el contenido publico del inicio.
    public interface IRegistrarPublicaciones
    {
        Task<ServiceResult> P_InsInicioContenido(DtoInicioContenidoCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetInicioContenidosList();
        Task<ServiceResult> F_GetInicioContenido(int idInicioContenido);
        Task<ServiceResult> P_UdpInicioContenido(int idInicioContenido, DtoInicioContenidoUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteInicioContenido(int idInicioContenido, AuditContext audit);
        Task<DtoInicioPublico> F_GetInicioPublico();
        Task<List<DtoInicioContenidoItem>> F_GetContenidoPublicoPorTipo(string tipoContenido);
        Task<DtoInicioContenidoItem?> F_GetContenidoPublicoDetalle(int idInicioContenido);
    }
}



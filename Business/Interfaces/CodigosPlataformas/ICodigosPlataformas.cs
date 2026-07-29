using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.CodigosPlataformas;

namespace Tienda_Streaming.Business.Interfaces.CodigosPlataformas
{
    public interface ICodigosPlataformas
    {
        Task<ServiceResult> F_GetCorreosAdminList();
        Task<ServiceResult> F_GetCorreoAdminDetalle(int idCorreo);
        Task<ServiceResult> P_DeleteCorreo(int idCorreo, AuditContext audit);
        Task<ServiceResult> F_BuscarCorreosPublico(DtoBuscarCorreoPlataformaRequest model);
        Task<ServiceResult> F_GetCorreoPublicoDetalle(DtoDetalleCorreoPlataformaRequest model);
        Task<int> SincronizarBuzon(CancellationToken cancellationToken);
        Task<int> EliminarCorreosAntiguos(CancellationToken cancellationToken);
    }
}

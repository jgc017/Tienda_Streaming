using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.Dominios;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Interfaces.Dominios
{
    // Contrato de negocio para el CRUD de dominios.
    public interface IDominios
    {
        Task<ServiceResult> P_InsDominio(DtoDominioCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetDominiosList(int idDominio);
        ServiceResult F_GetDominiosList();
        Task<ServiceResult> F_GetDominio(int idDominio);
        Task<ServiceResult> P_UdpDominio(int idDominio, DtoDominioUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteDominio(int idDominio, AuditContext audit);
    }
}

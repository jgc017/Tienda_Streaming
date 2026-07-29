using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.SistemaConfig;

namespace Tienda_Streaming.Business.Interfaces.SistemaConfig
{
    // Contrato del flujo de configuracion visual global del sistema.
    public interface ISistemaConfig
    {
        Task<DtoSistemaVisualConfigItem> F_GetSistemaVisualConfig();
        Task<ServiceResult> P_UdpSistemaVisualConfig(DtoSistemaVisualConfigUpdateRequest model, AuditContext audit);
    }
}

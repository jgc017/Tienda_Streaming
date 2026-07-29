using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.ImagenesProducto;

namespace Tienda_Streaming.Business.Interfaces.ImagenesProducto
{
    public interface IImagenesProducto
    {
        Task<ServiceResult> P_InsImagenProducto(DtoImagenProductoCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetImagenesProductoList();
        Task<ServiceResult> F_GetImagenProducto(int idImagenProducto);
        Task<ServiceResult> P_UdpImagenProducto(int idImagenProducto, DtoImagenProductoUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteImagenProducto(int idImagenProducto, AuditContext audit);
        Task<ServiceResult> P_MoverImagenProducto(DtoImagenProductoOrdenRequest model, AuditContext audit);
    }
}

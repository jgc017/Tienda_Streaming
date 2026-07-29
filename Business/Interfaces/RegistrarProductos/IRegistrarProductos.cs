using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarProductos;

namespace Tienda_Streaming.Business.Interfaces.RegistrarProductos
{
    public interface IRegistrarProductos
    {
        Task<ServiceResult> P_InsCuenta(DtoCuentaCreateRequest model, AuditContext audit);
        Task<ServiceResult> F_GetCuentasList();
        Task<ServiceResult> F_GetCuenta(int idCuenta);
        Task<ServiceResult> P_UdpCuenta(int idCuenta, DtoCuentaUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteCuenta(int idCuenta, AuditContext audit);
        Task<ServiceResult> P_ConfirmarCompraInterna(DtoConfirmarCompraRequest model, AuditContext audit);
        Task<ServiceResult> P_ConfirmarCompraPublica(DtoCompraPublicaRequest model, AuditContext audit);
        Task<ServiceResult> F_GetSaldoBilletera(int idUsuario);
        Task<ServiceResult> P_ValidarCodigoCompra(DtoValidarCodigoCompraRequest model);
        Task<ServiceResult> P_InsPrecioProducto(DtoPrecioProductoRequest model, AuditContext audit);
        Task<ServiceResult> F_GetPreciosProductoList();
        Task<ServiceResult> P_UdpPrecioProducto(int idPrecioProducto, DtoPrecioProductoRequest model, AuditContext audit);
        Task<ServiceResult> P_DeletePrecioProducto(int idPrecioProducto, AuditContext audit);
        Task<ServiceResult> P_InsCombo(DtoComboRequest model, AuditContext audit);
        Task<ServiceResult> F_GetCombosList();
        Task<ServiceResult> F_GetCombo(int idCombo);
        Task<ServiceResult> P_UdpCombo(int idCombo, DtoComboRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteCombo(int idCombo, AuditContext audit);
        Task<ServiceResult> P_RecargarBilletera(DtoRecargaBilleteraRequest model, AuditContext audit);
        Task<ServiceResult> F_GetBilleterasList();
        Task<ServiceResult> F_GetBilletera(int idBilletera);
        Task<ServiceResult> P_UdpBilletera(int idBilletera, DtoBilleteraUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_GenerarCodigoCompra(DtoCodigoCompraRequest model, AuditContext audit);
        Task<ServiceResult> F_GetCodigosCompraList();
        Task<ServiceResult> F_GetCodigoCompra(int idCodigoCompra);
        Task<ServiceResult> P_UdpCodigoCompra(int idCodigoCompra, DtoCodigoCompraUpdateRequest model, AuditContext audit);
        Task<ServiceResult> P_DeleteCodigoCompra(int idCodigoCompra, AuditContext audit);
        Task<ServiceResult> F_GetHistorialCompras(int? idUsuario, IEnumerable<int> rolesUsuario);
        Task<ServiceResult> F_GetDetalleCompra(int idPedido, int? idUsuario, IEnumerable<int> rolesUsuario);
        Task<ServiceResult> F_GetHistorialComprasCliente(DtoHistorialClienteRequest model);
        Task<ServiceResult> F_GetDetalleCompraCliente(DtoDetalleCompraRequest model);
        Task<List<DtoProductoTiendaItem>> F_GetProductosTienda(int idTipoUsuario, int idTipoImagen);
        Task<List<DtoComboItem>> F_GetCombosTienda(int idTipoUsuario);
    }
}

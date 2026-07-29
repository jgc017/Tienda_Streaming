using System;
using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.RegistrarProductos
{
    public class DtoCuentaCreateRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La plataforma es obligatoria")]
        public int Id_Plataforma { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El tipo de usuario es obligatorio")]
        public int Id_Tipo_Usuario { get; set; }

        [Range(1, 3650, ErrorMessage = "El tiempo de pantalla debe ser mayor a cero")]
        public int Tiempo_Pantalla { get; set; } = 30;

        [Required(ErrorMessage = "El correo de la cuenta es obligatorio")]
        [StringLength(160)]
        public string Correo_Cuenta { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasena de la cuenta es obligatoria")]
        [StringLength(160)]
        public string Contrasena_Cuenta { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Perfil_Cuenta { get; set; }

        [StringLength(20)]
        public string? Pin_Cuenta { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    public class DtoCuentaUpdateRequest : DtoCuentaCreateRequest
    {
    }

    public class DtoCuentaItem
    {
        public int Id_Cuenta { get; set; }
        public int Id_Plataforma { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public int Id_Tipo_Usuario { get; set; }
        public string TipoUsuario { get; set; } = string.Empty;
        public int Tiempo_Pantalla { get; set; }
        public string Correo_Cuenta { get; set; } = string.Empty;
        public string Contrasena_Cuenta { get; set; } = string.Empty;
        public string? Perfil_Cuenta { get; set; }
        public string? Pin_Cuenta { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
        public short Vigente { get; set; }
    }

    public class DtoProductoTiendaItem
    {
        public int Id_Plataforma { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Tiempo_Pantalla { get; set; }
        public int CantidadDisponible { get; set; }
        public string? ImagenUrl { get; set; }
        public int Orden { get; set; }
        public List<DtoProductoDuracionItem> OpcionesDuracion { get; set; } = new();
    }

    public class DtoProductoDuracionItem
    {
        public int Tiempo_Pantalla { get; set; }
        public int CantidadDisponible { get; set; }
        public decimal Precio { get; set; }
    }

    public class DtoConfirmarCompraRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El tipo de usuario es obligatorio")]
        public int Id_Tipo_Usuario { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        [StringLength(120)]
        public string Nombre_Cliente { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
        [StringLength(160)]
        public string? Correo_Cliente { get; set; }

        public DateTime Fecha_Compra { get; set; }

        [MinLength(1, ErrorMessage = "Debe seleccionar al menos un producto")]
        public List<DtoConfirmarCompraItem> Items { get; set; } = new();
    }

    public class DtoConfirmarCompraItem
    {
        public string Tipo_Producto { get; set; } = "Pantalla";

        [Range(1, int.MaxValue, ErrorMessage = "La plataforma es obligatoria")]
        public int? Id_Plataforma { get; set; }

        public int? Id_Combo { get; set; }

        [Range(1, 3650, ErrorMessage = "El tiempo de pantalla debe ser mayor a cero")]
        public int Tiempo_Pantalla { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero")]
        public int Cantidad { get; set; }
    }

    public class DtoCompraCuentaItem
    {
        public int Id_Cuenta { get; set; }
        public int Id_Plataforma { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string Correo_Cuenta { get; set; } = string.Empty;
        public string Contrasena_Cuenta { get; set; } = string.Empty;
        public string? Perfil_Cuenta { get; set; }
        public string? Pin_Cuenta { get; set; }
        public DateTime Fecha_Compra { get; set; }
        public DateTime? Fecha_Vencimiento { get; set; }
        public int Tiempo_Pantalla { get; set; }
    }

    public class DtoPrecioProductoRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La plataforma es obligatoria")]
        public int Id_Plataforma { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El tipo de usuario es obligatorio")]
        public int Id_Tipo_Usuario { get; set; }

        [Range(1, 3650, ErrorMessage = "Los dias deben ser mayores a cero")]
        public int Tiempo_Pantalla { get; set; }

        [Range(0.01, 9999999999, ErrorMessage = "El precio debe ser mayor a cero")]
        public decimal Precio { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    public class DtoPrecioProductoItem : DtoPrecioProductoRequest
    {
        public int Id_Precio_Producto { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
    }

    public class DtoComboRequest
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(500)]
        public string? ImagenUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El tipo de usuario es obligatorio")]
        public int Id_Tipo_Usuario { get; set; }

        [Range(1, 3650, ErrorMessage = "Los dias deben ser mayores a cero")]
        public int Tiempo_Pantalla { get; set; }

        [Range(0.01, 9999999999, ErrorMessage = "El precio debe ser mayor a cero")]
        public decimal Precio { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El orden debe ser mayor a cero")]
        public int Orden { get; set; } = 1;

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;

        [MinLength(1, ErrorMessage = "Debe agregar al menos una plataforma")]
        public List<DtoComboPlataformaRequest> Plataformas { get; set; } = new();
    }

    public class DtoComboPlataformaRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "La plataforma es obligatoria")]
        public int Id_Plataforma { get; set; }

        [Range(1, 100, ErrorMessage = "La cantidad debe ser mayor a cero")]
        public int Cantidad { get; set; } = 1;
    }

    public class DtoComboItem
    {
        public int Id_Combo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? ImagenUrl { get; set; }
        public int Id_Tipo_Usuario { get; set; }
        public string TipoUsuario { get; set; } = string.Empty;
        public int Tiempo_Pantalla { get; set; }
        public decimal Precio { get; set; }
        public int Orden { get; set; }
        public short Vigente { get; set; }
        public int CantidadDisponible { get; set; }
        public List<DtoComboPlataformaItem> Plataformas { get; set; } = new();
    }

    public class DtoComboPlataformaItem : DtoComboPlataformaRequest
    {
        public string Plataforma { get; set; } = string.Empty;
    }

    public class DtoRecargaBilleteraRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El vendedor es obligatorio")]
        public int Id_Usuario { get; set; }

        [Range(0.01, 9999999999, ErrorMessage = "El valor debe ser mayor a cero")]
        public decimal Valor { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }
    }

    public class DtoBilleteraUpdateRequest
    {
        [Range(0, 9999999999, ErrorMessage = "El saldo no puede ser negativo")]
        public decimal Saldo { get; set; }

        [StringLength(200)]
        public string? Descripcion { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    public class DtoBilleteraItem
    {
        public int Id_Billetera { get; set; }
        public int Id_Usuario { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
        public short Vigente { get; set; }
    }

    public class DtoCodigoCompraRequest
    {
        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        [StringLength(120)]
        public string Nombre_Cliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo del cliente es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
        [StringLength(160)]
        public string Correo_Cliente { get; set; } = string.Empty;

        [Range(0.01, 9999999999, ErrorMessage = "El valor debe ser mayor a cero")]
        public decimal Valor { get; set; }

        public DateTime? Fecha_Expiracion { get; set; }
    }

    public class DtoCodigoCompraUpdateRequest
    {
        [Required(ErrorMessage = "El nombre del cliente es obligatorio")]
        [StringLength(120)]
        public string Nombre_Cliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo del cliente es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
        [StringLength(160)]
        public string Correo_Cliente { get; set; } = string.Empty;

        [Range(0, 9999999999, ErrorMessage = "El saldo no puede ser negativo")]
        public decimal Valor { get; set; }

        public DateTime? Fecha_Expiracion { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }

    public class DtoCodigoCompraItem
    {
        public int Id_Codigo_Compra { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string Correo_Cliente { get; set; } = string.Empty;
        public decimal Valor_Inicial { get; set; }
        public decimal Saldo_Disponible { get; set; }
        public DateTime? Fecha_Expiracion { get; set; }
        public short Vigente { get; set; }
    }

    public class DtoValidarCodigoCompraRequest
    {
        [Required(ErrorMessage = "El codigo es obligatorio")]
        [StringLength(40)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo del codigo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato valido")]
        [StringLength(160)]
        public string Correo_Cliente { get; set; } = string.Empty;
    }

    public class DtoCompraPublicaRequest : DtoConfirmarCompraRequest
    {
        [Required(ErrorMessage = "El codigo de compra es obligatorio")]
        [StringLength(40)]
        public string Codigo_Compra { get; set; } = string.Empty;
    }

    public class DtoHistorialClienteRequest : DtoValidarCodigoCompraRequest
    {
    }

    public class DtoDetalleCompraRequest : DtoValidarCodigoCompraRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "El pedido es obligatorio")]
        public int Id_Pedido { get; set; }
    }

    public class DtoSaldoBilleteraItem
    {
        public decimal Saldo { get; set; }
    }

    public class DtoCompraResultado
    {
        public int Id_Pedido { get; set; }
        public decimal Total { get; set; }
        public decimal Saldo_Restante { get; set; }
        public List<DtoCompraCuentaItem> Cuentas { get; set; } = new();
        public List<DtoCompraDetalleResultado> Detalles { get; set; } = new();
    }

    public class DtoCompraDetalleResultado
    {
        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Valor_Unitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class DtoHistorialCompraItem
    {
        public int Id_Pedido { get; set; }
        public string Origen { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
        public string? Usuario { get; set; }
        public string? Codigo { get; set; }
        public string Nombre_Cliente { get; set; } = string.Empty;
        public string? Correo_Cliente { get; set; }
        public string Plataforma { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime Fecha_Compra { get; set; }
        public int CantidadCuentas { get; set; }
    }

    public class DtoDetalleCompraItem
    {
        public int Id_Pedido { get; set; }
        public decimal Total { get; set; }
        public decimal Saldo_Restante { get; set; }
        public List<DtoCompraCuentaItem> Cuentas { get; set; } = new();
        public List<DtoCompraDetalleResultado> Detalles { get; set; } = new();
    }
}


using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarProductos;
using Tienda_Streaming.Security;
using Tienda_Streaming.Services.Email;
using System.Net;
using System.Data;
using System.Security.Cryptography;
using UsuarioEntity = Tienda_Streaming.Models.Administracion.Usuarios;

namespace Tienda_Streaming.Business.Services.RegistrarProductos
{
    public class RegistrarProductosService : IRegistrarProductos
    {
        private const int DominioPlataformas = 10;
        private const int DominioTipoUsuario = 22;
        private const int TipoUsuarioCliente = 23;
        private const int TipoUsuarioVendedor = 24;
        private const int TipoPantallaIndividual = 35;
        private const int RolSuperUsuario = 1;
        private const int RolAdministrador = 2;
        private const string ProductoPantalla = "Pantalla";
        private const string ProductoCombo = "Combo";
        private const string NombreEmpresa = "Tienda Streaming";
        private readonly AppDbContext _context;
        private readonly ILogger<RegistrarProductosService> _logger;
        private readonly ICuentaPasswordProtector _cuentaPasswordProtector;
        private readonly IEmailSender _emailSender;

        public RegistrarProductosService(
            AppDbContext context,
            ILogger<RegistrarProductosService> logger,
            ICuentaPasswordProtector cuentaPasswordProtector,
            IEmailSender emailSender)
        {
            _context = context;
            _logger = logger;
            _cuentaPasswordProtector = cuentaPasswordProtector;
            _emailSender = emailSender;
        }

        public async Task<ServiceResult> P_InsCuenta(DtoCuentaCreateRequest model, AuditContext audit)
        {
            var validacion = await ValidarDominios(model.Id_Plataforma, model.Id_Tipo_Usuario);
            if (validacion != null)
            {
                return validacion;
            }

            var cuenta = new Cuentas
            {
                Id_Plataforma = model.Id_Plataforma,
                Id_Tipo_Usuario = model.Id_Tipo_Usuario,
                Tiempo_Pantalla = model.Tiempo_Pantalla,
                Correo_Cuenta = model.Correo_Cuenta.Trim(),
                Contrasena_Cuenta = _cuentaPasswordProtector.Protect(model.Contrasena_Cuenta),
                Perfil_Cuenta = model.Perfil_Cuenta?.Trim(),
                Pin_Cuenta = model.Pin_Cuenta?.Trim(),
                Vigente = model.Vigente,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            try
            {
                _context.Cuentas.Add(cuenta);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error registrando cuenta de producto {Correo}", model.Correo_Cuenta);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "No fue posible registrar el producto.");
            }

            return ServiceResult.Success(
                "Producto registrado correctamente.",
                auditDescription: $"Registro de cuenta producto {cuenta.Correo_Cuenta} con id {cuenta.Id_Cuenta}");
        }

        public async Task<ServiceResult> F_GetCuentasList()
        {
            // La administracion del inventario debe mostrar cuentas activas e inactivas.
            // Las tiendas usan consultas separadas con Vigente == 1 para calcular disponibilidad.
            var cuentas = await QueryCuentasInventario()
                .OrderBy(c => c.Plataforma!.Descripcion)
                .ThenBy(c => c.TipoUsuario!.Descripcion)
                .ThenBy(c => c.Vigente)
                .ThenByDescending(c => c.Fecha_Creacion)
                .ToListAsync();

            return ServiceResult.Success(data: cuentas.Select(c => MapCuenta(c, incluirContrasena: false)).ToList());
        }

        public async Task<ServiceResult> F_GetCuenta(int idCuenta)
        {
            var cuenta = await QueryCuentas().FirstOrDefaultAsync(c => c.Id_Cuenta == idCuenta);
            return cuenta == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Producto no encontrado.")
                : ServiceResult.Success(data: MapCuenta(cuenta, incluirContrasena: true), auditDescription: $"Consulta de cuenta producto {cuenta.Id_Cuenta}");
        }

        public async Task<ServiceResult> P_UdpCuenta(int idCuenta, DtoCuentaUpdateRequest model, AuditContext audit)
        {
            var cuenta = await _context.Cuentas.FindAsync(idCuenta);
            if (cuenta == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Producto no existe.");
            }

            var validacion = await ValidarDominios(model.Id_Plataforma, model.Id_Tipo_Usuario);
            if (validacion != null)
            {
                return validacion;
            }

            cuenta.Id_Plataforma = model.Id_Plataforma;
            cuenta.Id_Tipo_Usuario = model.Id_Tipo_Usuario;
            cuenta.Tiempo_Pantalla = model.Tiempo_Pantalla;
            cuenta.Correo_Cuenta = model.Correo_Cuenta.Trim();
            cuenta.Contrasena_Cuenta = _cuentaPasswordProtector.Protect(model.Contrasena_Cuenta);
            cuenta.Perfil_Cuenta = model.Perfil_Cuenta?.Trim();
            cuenta.Pin_Cuenta = model.Pin_Cuenta?.Trim();
            cuenta.Vigente = model.Vigente;
            cuenta.Id_Usuario_Modifica = audit.UserId;
            cuenta.Fecha_Modifica = DateTime.UtcNow;
            cuenta.Maquina_Modifica = audit.Machine;

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Producto actualizado correctamente.",
                auditDescription: $"Actualizacion de cuenta producto {cuenta.Id_Cuenta}");
        }

        public async Task<ServiceResult> P_DeleteCuenta(int idCuenta, AuditContext audit)
        {
            var cuenta = await _context.Cuentas.FindAsync(idCuenta);
            if (cuenta == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Producto no existe.");
            }

            cuenta.Vigente = 0;
            cuenta.Id_Usuario_Modifica = audit.UserId;
            cuenta.Fecha_Modifica = DateTime.UtcNow;
            cuenta.Maquina_Modifica = audit.Machine;
            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Producto marcado como inactivo correctamente.",
                auditDescription: $"Eliminacion logica de cuenta producto {cuenta.Id_Cuenta}");
        }

        public async Task<ServiceResult> P_InsPrecioProducto(DtoPrecioProductoRequest model, AuditContext audit)
        {
            var validacion = await ValidarDominios(model.Id_Plataforma, model.Id_Tipo_Usuario);
            if (validacion != null)
            {
                return validacion;
            }

            var existe = await _context.PreciosProducto.AnyAsync(p =>
                p.Id_Plataforma == model.Id_Plataforma &&
                p.Id_Tipo_Usuario == model.Id_Tipo_Usuario &&
                p.Tiempo_Pantalla == model.Tiempo_Pantalla);

            if (existe)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un precio para esa plataforma, tipo de usuario y dias.");
            }

            var precio = new PreciosProducto
            {
                Id_Plataforma = model.Id_Plataforma,
                Id_Tipo_Usuario = model.Id_Tipo_Usuario,
                Tiempo_Pantalla = model.Tiempo_Pantalla,
                Precio = decimal.Round(model.Precio, 2),
                Vigente = model.Vigente,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.PreciosProducto.Add(precio);
            await _context.SaveChangesAsync();
            return ServiceResult.Success("Precio registrado correctamente.", auditDescription: $"Registro de precio producto {precio.Id_Precio_Producto}");
        }

        public async Task<ServiceResult> F_GetPreciosProductoList()
        {
            var data = await _context.PreciosProducto
                .AsNoTracking()
                .Include(p => p.Plataforma)
                .Include(p => p.TipoUsuario)
                .OrderBy(p => p.Plataforma!.Descripcion)
                .ThenBy(p => p.TipoUsuario!.Descripcion)
                .ThenBy(p => p.Tiempo_Pantalla)
                .Select(p => new DtoPrecioProductoItem
                {
                    Id_Precio_Producto = p.Id_Precio_Producto,
                    Id_Plataforma = p.Id_Plataforma,
                    Plataforma = p.Plataforma!.Descripcion,
                    Id_Tipo_Usuario = p.Id_Tipo_Usuario,
                    TipoUsuario = p.TipoUsuario!.Descripcion,
                    Tiempo_Pantalla = p.Tiempo_Pantalla,
                    Precio = p.Precio,
                    Vigente = p.Vigente
                })
                .ToListAsync();

            return ServiceResult.Success(data: data);
        }

        public async Task<ServiceResult> P_UdpPrecioProducto(int idPrecioProducto, DtoPrecioProductoRequest model, AuditContext audit)
        {
            var precio = await _context.PreciosProducto.FindAsync(idPrecioProducto);
            if (precio == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Precio no existe.");
            }

            var validacion = await ValidarDominios(model.Id_Plataforma, model.Id_Tipo_Usuario);
            if (validacion != null)
            {
                return validacion;
            }

            var existe = await _context.PreciosProducto.AnyAsync(p =>
                p.Id_Precio_Producto != idPrecioProducto &&
                p.Id_Plataforma == model.Id_Plataforma &&
                p.Id_Tipo_Usuario == model.Id_Tipo_Usuario &&
                p.Tiempo_Pantalla == model.Tiempo_Pantalla);

            if (existe)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un precio para esa plataforma, tipo de usuario y dias.");
            }

            precio.Id_Plataforma = model.Id_Plataforma;
            precio.Id_Tipo_Usuario = model.Id_Tipo_Usuario;
            precio.Tiempo_Pantalla = model.Tiempo_Pantalla;
            precio.Precio = decimal.Round(model.Precio, 2);
            precio.Vigente = model.Vigente;
            precio.Id_Usuario_Modifica = audit.UserId;
            precio.Fecha_Modifica = DateTime.UtcNow;
            precio.Maquina_Modifica = audit.Machine;
            await _context.SaveChangesAsync();

            return ServiceResult.Success("Precio actualizado correctamente.", auditDescription: $"Actualizacion de precio producto {precio.Id_Precio_Producto}");
        }

        public async Task<ServiceResult> P_DeletePrecioProducto(int idPrecioProducto, AuditContext audit)
        {
            var precio = await _context.PreciosProducto.FindAsync(idPrecioProducto);
            if (precio == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Precio no existe.");
            }

            precio.Vigente = 0;
            precio.Id_Usuario_Modifica = audit.UserId;
            precio.Fecha_Modifica = DateTime.UtcNow;
            precio.Maquina_Modifica = audit.Machine;
            await _context.SaveChangesAsync();

            return ServiceResult.Success("Precio marcado como inactivo correctamente.", auditDescription: $"Eliminacion logica de precio producto {idPrecioProducto}");
        }

        public async Task<ServiceResult> P_InsCombo(DtoComboRequest model, AuditContext audit)
        {
            var combo = await CrearComboDesdeRequest(model, audit);
            if (!combo.Ok)
            {
                return combo.Result;
            }

            _context.Combos.Add(combo.Combo!);
            await _context.SaveChangesAsync();
            return ServiceResult.Success("Combo registrado correctamente.", auditDescription: $"Registro de combo {combo.Combo!.Nombre}");
        }

        public async Task<ServiceResult> F_GetCombosList()
        {
            var combos = await _context.Combos
                .AsNoTracking()
                .Include(c => c.TipoUsuario)
                .Include(c => c.Plataformas)
                    .ThenInclude(p => p.Plataforma)
                .OrderBy(c => c.Orden)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            return ServiceResult.Success(data: combos.Select(MapCombo).ToList());
        }

        public async Task<ServiceResult> F_GetCombo(int idCombo)
        {
            var combo = await _context.Combos
                .AsNoTracking()
                .Include(c => c.TipoUsuario)
                .Include(c => c.Plataformas)
                    .ThenInclude(p => p.Plataforma)
                .FirstOrDefaultAsync(c => c.Id_Combo == idCombo);

            return combo == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Combo no encontrado.")
                : ServiceResult.Success(data: MapCombo(combo), auditDescription: $"Consulta de combo {combo.Id_Combo}");
        }

        public async Task<ServiceResult> P_UdpCombo(int idCombo, DtoComboRequest model, AuditContext audit)
        {
            var combo = await _context.Combos
                .Include(c => c.Plataformas)
                .FirstOrDefaultAsync(c => c.Id_Combo == idCombo);

            if (combo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Combo no existe.");
            }

            var nuevo = await CrearComboDesdeRequest(model, audit, idCombo);
            if (!nuevo.Ok)
            {
                return nuevo.Result;
            }

            combo.Nombre = model.Nombre.Trim();
            combo.Descripcion = model.Descripcion?.Trim();
            combo.ImagenUrl = model.ImagenUrl?.Trim();
            combo.Id_Tipo_Usuario = model.Id_Tipo_Usuario;
            combo.Tiempo_Pantalla = model.Tiempo_Pantalla;
            combo.Precio = decimal.Round(model.Precio, 2);
            combo.Orden = model.Orden;
            combo.Vigente = model.Vigente;
            combo.Id_Usuario_Modifica = audit.UserId;
            combo.Fecha_Modifica = DateTime.UtcNow;
            combo.Maquina_Modifica = audit.Machine;

            _context.ComboPlataformas.RemoveRange(combo.Plataformas);
            combo.Plataformas = nuevo.Combo!.Plataformas;
            await _context.SaveChangesAsync();

            return ServiceResult.Success("Combo actualizado correctamente.", auditDescription: $"Actualizacion de combo {combo.Id_Combo}");
        }

        public async Task<ServiceResult> P_DeleteCombo(int idCombo, AuditContext audit)
        {
            var combo = await _context.Combos.FindAsync(idCombo);
            if (combo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Combo no existe.");
            }

            combo.Vigente = 0;
            combo.Id_Usuario_Modifica = audit.UserId;
            combo.Fecha_Modifica = DateTime.UtcNow;
            combo.Maquina_Modifica = audit.Machine;
            await _context.SaveChangesAsync();

            return ServiceResult.Success("Combo marcado como inactivo correctamente.", auditDescription: $"Eliminacion logica de combo {idCombo}");
        }

        public async Task<ServiceResult> P_RecargarBilletera(DtoRecargaBilleteraRequest model, AuditContext audit)
        {
            var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id_Usuario == model.Id_Usuario && u.Vigente == 1);
            if (usuario == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "El vendedor no existe o esta inactivo.");
            }

            var billetera = await _context.BilleteraVendedores.FirstOrDefaultAsync(b => b.Id_Usuario == model.Id_Usuario);
            if (billetera == null)
            {
                billetera = new BilleteraVendedores
                {
                    Id_Usuario = model.Id_Usuario,
                    Saldo = 0m,
                    Vigente = 1,
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                };
                _context.BilleteraVendedores.Add(billetera);
                await _context.SaveChangesAsync();
            }

            var valorRecargado = decimal.Round(model.Valor, 2);
            var saldoAnterior = billetera.Saldo;
            billetera.Saldo += valorRecargado;
            billetera.Vigente = 1;
            billetera.Id_Usuario_Modifica = audit.UserId;
            billetera.Fecha_Modifica = DateTime.UtcNow;
            billetera.Maquina_Modifica = audit.Machine;

            _context.MovimientosBilletera.Add(new MovimientosBilletera
            {
                Id_Billetera = billetera.Id_Billetera,
                Tipo_Movimiento = "Recarga",
                Valor = valorRecargado,
                Saldo_Anterior = saldoAnterior,
                Saldo_Nuevo = billetera.Saldo,
                Descripcion = model.Descripcion?.Trim(),
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            });

            await _context.SaveChangesAsync();
            await EnviarCorreoRecargaBilleteraSiAplica(usuario, valorRecargado, saldoAnterior, billetera.Saldo, model.Descripcion);

            return ServiceResult.Success("Saldo recargado correctamente.", data: new DtoSaldoBilleteraItem { Saldo = billetera.Saldo }, auditDescription: $"Recarga billetera usuario {model.Id_Usuario}");
        }

        public async Task<ServiceResult> F_GetBilleterasList()
        {
            var data = await _context.BilleteraVendedores
                .AsNoTracking()
                .Include(b => b.Usuario)
                .OrderBy(b => b.Usuario!.Nombre)
                .Select(b => new DtoBilleteraItem
                {
                    Id_Billetera = b.Id_Billetera,
                    Id_Usuario = b.Id_Usuario,
                    Usuario = b.Usuario!.Usuario,
                    Nombre = b.Usuario.Nombre,
                    Saldo = b.Saldo,
                    Vigente = b.Vigente
                })
                .ToListAsync();

            return ServiceResult.Success(data: data);
        }

        public async Task<ServiceResult> F_GetBilletera(int idBilletera)
        {
            var billetera = await _context.BilleteraVendedores
                .AsNoTracking()
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.Id_Billetera == idBilletera);

            return billetera == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Billetera no existe.")
                : ServiceResult.Success(data: new DtoBilleteraItem
                {
                    Id_Billetera = billetera.Id_Billetera,
                    Id_Usuario = billetera.Id_Usuario,
                    Usuario = billetera.Usuario?.Usuario ?? string.Empty,
                    Nombre = billetera.Usuario?.Nombre ?? string.Empty,
                    Saldo = billetera.Saldo,
                    Vigente = billetera.Vigente
                });
        }

        public async Task<ServiceResult> P_UdpBilletera(int idBilletera, DtoBilleteraUpdateRequest model, AuditContext audit)
        {
            var billetera = await _context.BilleteraVendedores.FirstOrDefaultAsync(b => b.Id_Billetera == idBilletera);
            if (billetera == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Billetera no existe.");
            }

            var saldoAnterior = billetera.Saldo;
            var saldoNuevo = decimal.Round(model.Saldo, 2);
            billetera.Saldo = saldoNuevo;
            billetera.Vigente = model.Vigente;
            billetera.Id_Usuario_Modifica = audit.UserId;
            billetera.Fecha_Modifica = DateTime.UtcNow;
            billetera.Maquina_Modifica = audit.Machine;

            if (saldoAnterior != saldoNuevo)
            {
                _context.MovimientosBilletera.Add(new MovimientosBilletera
                {
                    Id_Billetera = billetera.Id_Billetera,
                    Tipo_Movimiento = "Ajuste",
                    Valor = saldoNuevo - saldoAnterior,
                    Saldo_Anterior = saldoAnterior,
                    Saldo_Nuevo = saldoNuevo,
                    Descripcion = model.Descripcion?.Trim(),
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                });
            }

            await _context.SaveChangesAsync();
            return ServiceResult.Success("Billetera actualizada correctamente.", data: new DtoSaldoBilleteraItem { Saldo = billetera.Saldo }, auditDescription: $"Actualizacion billetera {idBilletera}");
        }

        public async Task<ServiceResult> F_GetSaldoBilletera(int idUsuario)
        {
            var saldo = await _context.BilleteraVendedores
                .AsNoTracking()
                .Where(b => b.Id_Usuario == idUsuario && b.Vigente == 1)
                .Select(b => (decimal?)b.Saldo)
                .FirstOrDefaultAsync() ?? 0m;

            return ServiceResult.Success(data: new DtoSaldoBilleteraItem { Saldo = saldo });
        }

        public async Task<ServiceResult> P_GenerarCodigoCompra(DtoCodigoCompraRequest model, AuditContext audit)
        {
            var codigo = await GenerarCodigoUnico();
            var nombreCliente = model.Nombre_Cliente.Trim();
            var correoCliente = NormalizarCorreo(model.Correo_Cliente) ?? string.Empty;
            var nuevo = new CodigosCompra
            {
                Codigo = codigo,
                Nombre_Cliente = nombreCliente,
                Correo_Cliente = correoCliente,
                Valor_Inicial = decimal.Round(model.Valor, 2),
                Saldo_Disponible = decimal.Round(model.Valor, 2),
                Fecha_Expiracion = model.Fecha_Expiracion,
                Vigente = 1,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.CodigosCompra.Add(nuevo);
            await _context.SaveChangesAsync();
            await EnviarCorreoCodigoSiAplica(nuevo);
            return ServiceResult.Success("Codigo generado correctamente.", data: MapCodigo(nuevo), auditDescription: $"Generacion de codigo compra {nuevo.Codigo}");
        }

        public async Task<ServiceResult> F_GetCodigosCompraList()
        {
            var data = await _context.CodigosCompra
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha_Creacion)
                .Select(c => MapCodigo(c))
                .ToListAsync();

            return ServiceResult.Success(data: data);
        }

        public async Task<ServiceResult> F_GetCodigoCompra(int idCodigoCompra)
        {
            var codigo = await _context.CodigosCompra
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id_Codigo_Compra == idCodigoCompra);

            return codigo == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Codigo no existe.")
                : ServiceResult.Success(data: MapCodigo(codigo));
        }

        public async Task<ServiceResult> P_UdpCodigoCompra(int idCodigoCompra, DtoCodigoCompraUpdateRequest model, AuditContext audit)
        {
            var codigo = await _context.CodigosCompra.FindAsync(idCodigoCompra);
            if (codigo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Codigo no existe.");
            }

            codigo.Nombre_Cliente = model.Nombre_Cliente.Trim();
            codigo.Correo_Cliente = NormalizarCorreo(model.Correo_Cliente) ?? string.Empty;
            codigo.Valor_Inicial = decimal.Round(model.Valor, 2);
            codigo.Saldo_Disponible = decimal.Round(model.Valor, 2);
            codigo.Fecha_Expiracion = model.Fecha_Expiracion;
            codigo.Vigente = model.Vigente;
            codigo.Id_Usuario_Modifica = audit.UserId;
            codigo.Fecha_Modifica = DateTime.UtcNow;
            codigo.Maquina_Modifica = audit.Machine;

            await _context.SaveChangesAsync();
            return ServiceResult.Success("Codigo actualizado correctamente.", data: MapCodigo(codigo), auditDescription: $"Actualizacion de codigo compra {codigo.Codigo}");
        }

        public async Task<ServiceResult> P_DeleteCodigoCompra(int idCodigoCompra, AuditContext audit)
        {
            var codigo = await _context.CodigosCompra.FindAsync(idCodigoCompra);
            if (codigo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Codigo no existe.");
            }

            codigo.Vigente = 0;
            codigo.Id_Usuario_Modifica = audit.UserId;
            codigo.Fecha_Modifica = DateTime.UtcNow;
            codigo.Maquina_Modifica = audit.Machine;
            await _context.SaveChangesAsync();
            return ServiceResult.Success("Codigo marcado como inactivo correctamente.", auditDescription: $"Eliminacion logica de codigo compra {codigo.Codigo}");
        }

        public async Task<ServiceResult> P_ValidarCodigoCompra(DtoValidarCodigoCompraRequest model)
        {
            var codigo = await ObtenerCodigoValido(model.Codigo, model.Correo_Cliente);
            if (codigo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "El codigo no existe, no corresponde al correo, esta vencido o no tiene saldo disponible.");
            }

            return ServiceResult.Success("Codigo validado correctamente.", data: MapCodigo(codigo));
        }

        public Task<ServiceResult> P_ConfirmarCompraInterna(DtoConfirmarCompraRequest model, AuditContext audit)
        {
            return ConfirmarCompra(model, audit, "Interna", codigoCompra: null, idUsuario: audit.UserId);
        }

        public async Task<ServiceResult> P_ConfirmarCompraPublica(DtoCompraPublicaRequest model, AuditContext audit)
        {
            var codigo = await ObtenerCodigoValido(model.Codigo_Compra, model.Correo_Cliente);
            if (codigo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "El codigo no existe, no corresponde al correo, esta vencido o no tiene saldo disponible.");
            }

            model.Nombre_Cliente = codigo.Nombre_Cliente;
            model.Correo_Cliente = codigo.Correo_Cliente;
            return await ConfirmarCompra(model, audit, "Publica", codigo, idUsuario: null);
        }

        public async Task<ServiceResult> F_GetHistorialCompras(int? idUsuario, IEnumerable<int> rolesUsuario)
        {
            var roles = rolesUsuario.Distinct().ToList();
            var puedeVerTodo = roles.Contains(RolSuperUsuario) || roles.Contains(RolAdministrador);
            var query = _context.Pedidos
                .AsNoTracking()
                .Include(p => p.TipoUsuario)
                .Include(p => p.Usuario)
                .Include(p => p.CodigoCompra)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Plataforma)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Combo)
                .Include(p => p.Cuentas)
                .AsQueryable();

            if (!puedeVerTodo)
            {
                if (!idUsuario.HasValue)
                {
                    return ServiceResult.Fail(StatusCodes.Status401Unauthorized, "No fue posible identificar el usuario.");
                }

                query = query.Where(p => p.Id_Usuario == idUsuario.Value);
            }

            var pedidos = await query
                .OrderByDescending(p => p.Fecha_Compra)
                .ToListAsync();

            var data = pedidos.Select(MapHistorial).ToList();
            return ServiceResult.Success(data: data);
        }

        public async Task<ServiceResult> F_GetDetalleCompra(int idPedido, int? idUsuario, IEnumerable<int> rolesUsuario)
        {
            var roles = rolesUsuario.Distinct().ToList();
            var puedeVerTodo = roles.Contains(RolSuperUsuario) || roles.Contains(RolAdministrador);
            var query = QueryDetallePedido().Where(p => p.Id_Pedido == idPedido);

            if (!puedeVerTodo)
            {
                if (!idUsuario.HasValue)
                {
                    return ServiceResult.Fail(StatusCodes.Status401Unauthorized, "No fue posible identificar el usuario.");
                }

                query = query.Where(p => p.Id_Usuario == idUsuario.Value);
            }

            var pedido = await query.FirstOrDefaultAsync();
            return pedido == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "No se encontro el pedido.")
                : ServiceResult.Success(data: MapDetalleCompra(pedido));
        }

        public async Task<ServiceResult> F_GetHistorialComprasCliente(DtoHistorialClienteRequest model)
        {
            var codigo = await ObtenerCodigoPorCorreo(model.Codigo, model.Correo_Cliente);
            if (codigo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "No se encontro un codigo asociado a ese correo.");
            }

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.TipoUsuario)
                .Include(p => p.CodigoCompra)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Plataforma)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Combo)
                .Include(p => p.Cuentas)
                .Where(p => p.Id_Codigo_Compra == codigo.Id_Codigo_Compra)
                .OrderByDescending(p => p.Fecha_Compra)
                .ToListAsync();

            return ServiceResult.Success(data: pedidos.Select(MapHistorial).ToList());
        }

        public async Task<ServiceResult> F_GetDetalleCompraCliente(DtoDetalleCompraRequest model)
        {
            var codigo = await ObtenerCodigoPorCorreo(model.Codigo, model.Correo_Cliente);
            if (codigo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "No se encontro un codigo asociado a ese correo.");
            }

            var pedido = await QueryDetallePedido()
                .Where(p => p.Id_Pedido == model.Id_Pedido && p.Id_Codigo_Compra == codigo.Id_Codigo_Compra)
                .FirstOrDefaultAsync();

            return pedido == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "No se encontro el pedido.")
                : ServiceResult.Success(data: MapDetalleCompra(pedido));
        }

        public async Task<List<DtoProductoTiendaItem>> F_GetProductosTienda(int idTipoUsuario, int idTipoImagen)
        {
            var stock = await _context.Cuentas
                .AsNoTracking()
                .Where(c => c.Vigente == 1 && c.Id_Tipo_Usuario == idTipoUsuario)
                .GroupBy(c => new { c.Id_Plataforma, c.Tiempo_Pantalla })
                .Select(g => new
                {
                    g.Key.Id_Plataforma,
                    g.Key.Tiempo_Pantalla,
                    Cantidad = g.Count(),
                })
                .ToListAsync();

            var stockPorPlataforma = stock
                .GroupBy(s => s.Id_Plataforma)
                .ToDictionary(g => g.Key, g => g.ToList());

            var precios = await _context.PreciosProducto
                .AsNoTracking()
                .Where(p => p.Vigente == 1 && p.Id_Tipo_Usuario == idTipoUsuario)
                .ToListAsync();

            var preciosPorPlataforma = precios
                .GroupBy(p => p.Id_Plataforma)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Tiempo_Pantalla).ToList());

            var imagenes = await _context.ImagenesProducto
                .AsNoTracking()
                .Include(i => i.Plataforma)
                .Where(i => i.Vigente == 1
                    && (i.Id_Tipo_Imagen ?? TipoPantallaIndividual) == idTipoImagen
                    && i.Plataforma != null
                    && i.Plataforma.Vigente == 1
                    && i.Plataforma.Id_Padre == DominioPlataformas)
                .OrderBy(i => i.Orden)
                .ThenBy(i => i.Fecha_Creacion)
                .ToListAsync();

            return imagenes
                .Select(imagen =>
                {
                    stockPorPlataforma.TryGetValue(imagen.Id_Plataforma, out var stockPlataforma);
                    preciosPorPlataforma.TryGetValue(imagen.Id_Plataforma, out var preciosPlataforma);

                    var opciones = preciosPlataforma == null
                        ? new List<DtoProductoDuracionItem>()
                        : preciosPlataforma
                            .Select(precio =>
                            {
                                var cantidad = stockPlataforma?
                                    .Where(s => s.Tiempo_Pantalla == precio.Tiempo_Pantalla)
                                    .Sum(s => s.Cantidad) ?? 0;

                                return new DtoProductoDuracionItem
                                {
                                    Tiempo_Pantalla = precio.Tiempo_Pantalla,
                                    CantidadDisponible = cantidad,
                                    Precio = precio.Precio
                                };
                            })
                            .ToList();

                    return new DtoProductoTiendaItem
                    {
                        Id_Plataforma = imagen.Id_Plataforma,
                        Plataforma = imagen.Plataforma?.Descripcion ?? string.Empty,
                        Descripcion = imagen.Descripcion ?? string.Empty,
                        Precio = opciones.Any() ? opciones.Min(o => o.Precio) : 0m,
                        Tiempo_Pantalla = opciones.Any() ? opciones.Min(o => o.Tiempo_Pantalla) : 0,
                        CantidadDisponible = opciones.Sum(o => o.CantidadDisponible),
                        ImagenUrl = imagen.ImagenUrl,
                        OpcionesDuracion = opciones,
                        Orden = imagen.Orden
                    };
                })
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Plataforma)
                .ToList();
        }

        public async Task<List<DtoComboItem>> F_GetCombosTienda(int idTipoUsuario)
        {
            var combos = await _context.Combos
                .AsNoTracking()
                .Include(c => c.TipoUsuario)
                .Include(c => c.Plataformas)
                    .ThenInclude(p => p.Plataforma)
                .Where(c => c.Vigente == 1 && c.Id_Tipo_Usuario == idTipoUsuario)
                .OrderBy(c => c.Orden)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            var disponibilidad = await CalcularDisponibilidadCombos(combos);
            return combos.Select(c =>
            {
                var item = MapCombo(c);
                item.CantidadDisponible = disponibilidad.GetValueOrDefault(c.Id_Combo);
                return item;
            }).ToList();
        }

        private async Task<ServiceResult> ConfirmarCompra(DtoConfirmarCompraRequest model, AuditContext audit, string origen, CodigosCompra? codigoCompra, int? idUsuario)
        {
            if (model.Items == null || !model.Items.Any())
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Debe seleccionar al menos un producto.");
            }

            var tipoUsuario = origen == "Publica" ? TipoUsuarioCliente : TipoUsuarioVendedor;
            if (model.Id_Tipo_Usuario != tipoUsuario)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El tipo de tienda no corresponde con la compra.");
            }

            var comprador = await ObtenerComprador(model, origen, idUsuario);
            if (!comprador.Ok)
            {
                return comprador.Result;
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var fechaCompra = NormalizarFechaPedido(model.Fecha_Compra);
            var lineas = await ConstruirLineasCompra(model.Items, tipoUsuario);
            if (!lineas.Ok)
            {
                return lineas.Result;
            }

            var total = lineas.Lineas.Sum(l => l.Subtotal);
            BilleteraVendedores? billetera = null;
            decimal saldoRestante;

            if (origen == "Publica")
            {
                codigoCompra = await _context.CodigosCompra.FirstOrDefaultAsync(c => c.Id_Codigo_Compra == codigoCompra!.Id_Codigo_Compra);
                if (codigoCompra == null || !CodigoTieneSaldo(codigoCompra))
                {
                    return ServiceResult.Fail(StatusCodes.Status404NotFound, "El codigo no existe, esta vencido o no tiene saldo disponible.");
                }

                if (codigoCompra.Saldo_Disponible < total)
                {
                    return ServiceResult.Fail(StatusCodes.Status409Conflict, $"El saldo del codigo no alcanza para esta compra. Saldo disponible: {codigoCompra.Saldo_Disponible:C0}.");
                }

                saldoRestante = codigoCompra.Saldo_Disponible - total;
                codigoCompra.Saldo_Disponible = saldoRestante;
                codigoCompra.Vigente = saldoRestante > 0 ? (short)1 : (short)0;
                codigoCompra.Id_Usuario_Modifica = audit.UserId;
                codigoCompra.Fecha_Modifica = DateTime.UtcNow;
                codigoCompra.Maquina_Modifica = audit.Machine;
            }
            else
            {
                if (!idUsuario.HasValue)
                {
                    return ServiceResult.Fail(StatusCodes.Status401Unauthorized, "No fue posible identificar el vendedor.");
                }

                billetera = await _context.BilleteraVendedores.FirstOrDefaultAsync(b => b.Id_Usuario == idUsuario.Value && b.Vigente == 1);
                if (billetera == null || billetera.Saldo <= 0)
                {
                    return ServiceResult.Fail(StatusCodes.Status409Conflict, "El vendedor no tiene saldo disponible en la billetera.");
                }

                if (billetera.Saldo < total)
                {
                    return ServiceResult.Fail(StatusCodes.Status409Conflict, $"El saldo de la billetera no alcanza para esta compra. Saldo disponible: {billetera.Saldo:C0}.");
                }

                saldoRestante = billetera.Saldo - total;
                var saldoAnterior = billetera.Saldo;
                billetera.Saldo = saldoRestante;
                billetera.Id_Usuario_Modifica = audit.UserId;
                billetera.Fecha_Modifica = DateTime.UtcNow;
                billetera.Maquina_Modifica = audit.Machine;

                _context.MovimientosBilletera.Add(new MovimientosBilletera
                {
                    Id_Billetera = billetera.Id_Billetera,
                    Tipo_Movimiento = "Compra",
                    Valor = total,
                    Saldo_Anterior = saldoAnterior,
                    Saldo_Nuevo = saldoRestante,
                    Descripcion = "Compra de productos",
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                });
            }

            var pedido = new Pedidos
            {
                Origen = origen,
                Id_Tipo_Usuario = tipoUsuario,
                Id_Usuario = idUsuario,
                Id_Codigo_Compra = codigoCompra?.Id_Codigo_Compra,
                Nombre_Cliente = comprador.Nombre,
                Correo_Cliente = comprador.Correo,
                Total = total,
                Fecha_Compra = fechaCompra,
                Vigente = 1,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            if (billetera != null)
            {
                foreach (var movimiento in _context.ChangeTracker.Entries<MovimientosBilletera>().Where(e => e.Entity.Id_Pedido == null))
                {
                    movimiento.Entity.Id_Pedido = pedido.Id_Pedido;
                }
            }

            var cuentasAsignadas = new List<Cuentas>();
            var contrasenasAsignadas = new Dictionary<int, string>();
            foreach (var linea in lineas.Lineas)
            {
                var detalle = new PedidoDetalles
                {
                    Id_Pedido = pedido.Id_Pedido,
                    Tipo_Producto = linea.TipoProducto,
                    Id_Plataforma = linea.IdPlataforma,
                    Id_Combo = linea.IdCombo,
                    Tiempo_Pantalla = linea.TiempoPantalla,
                    Cantidad = linea.Cantidad,
                    Precio_Unitario = linea.PrecioUnitario,
                    Subtotal = linea.Subtotal
                };

                _context.PedidoDetalles.Add(detalle);
                await _context.SaveChangesAsync();

                var cuentasLinea = await TomarCuentasDisponibles(linea, tipoUsuario);
                if (cuentasLinea.Count < linea.TotalCuentasRequeridas)
                {
                    return ServiceResult.Fail(StatusCodes.Status409Conflict, "No hay suficientes cuentas disponibles para uno o mas productos.");
                }

                var errorContrasena = ValidarContrasenasCompra(cuentasLinea, contrasenasAsignadas);
                if (errorContrasena != null)
                {
                    return errorContrasena;
                }

                foreach (var cuenta in cuentasLinea)
                {
                    cuenta.Fecha_Vencimiento = CalcularFechaVencimiento(fechaCompra, cuenta.Tiempo_Pantalla);
                    cuenta.Vigente = 0;
                    cuenta.Id_Usuario_Modifica = audit.UserId;
                    cuenta.Fecha_Modifica = DateTime.UtcNow;
                    cuenta.Maquina_Modifica = audit.Machine;

                    _context.PedidoCuentas.Add(new PedidoCuentas
                    {
                        Id_Pedido = pedido.Id_Pedido,
                        Id_Pedido_Detalle = detalle.Id_Pedido_Detalle,
                        Id_Cuenta = cuenta.Id_Cuenta
                    });
                }

                cuentasAsignadas.AddRange(cuentasLinea);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var data = new DtoCompraResultado
            {
                Id_Pedido = pedido.Id_Pedido,
                Total = total,
                Saldo_Restante = saldoRestante,
                Detalles = lineas.Lineas.Select(l => new DtoCompraDetalleResultado
                {
                    Producto = l.NombreProducto,
                    Cantidad = l.Cantidad,
                    Valor_Unitario = l.PrecioUnitario,
                    Subtotal = l.Subtotal
                }).ToList(),
                Cuentas = cuentasAsignadas
                    .OrderBy(c => c.Plataforma?.Descripcion)
                    .ThenBy(c => c.Id_Cuenta)
                    .Select(c => new DtoCompraCuentaItem
                    {
                        Id_Cuenta = c.Id_Cuenta,
                        Id_Plataforma = c.Id_Plataforma,
                        Plataforma = c.Plataforma?.Descripcion ?? string.Empty,
                        Correo_Cuenta = c.Correo_Cuenta,
                        Contrasena_Cuenta = contrasenasAsignadas.TryGetValue(c.Id_Cuenta, out var contrasena) ? contrasena : string.Empty,
                        Perfil_Cuenta = c.Perfil_Cuenta,
                        Pin_Cuenta = c.Pin_Cuenta,
                        Fecha_Compra = fechaCompra,
                        Fecha_Vencimiento = c.Fecha_Vencimiento,
                        Tiempo_Pantalla = c.Tiempo_Pantalla
                    })
                    .ToList()
            };

            await EnviarCorreoCompraSiAplica(comprador.Correo, comprador.Nombre, origen, data);

            return ServiceResult.Success(
                "Compra confirmada correctamente.",
                data: data,
                auditDescription: $"Compra {origen} confirmada. Pedido {pedido.Id_Pedido}. Cuentas inactivadas: {cuentasAsignadas.Count}");
        }

        private ServiceResult? ValidarContrasenasCompra(IEnumerable<Cuentas> cuentas, Dictionary<int, string> contrasenas)
        {
            foreach (var cuenta in cuentas)
            {
                if (_cuentaPasswordProtector.TryUnprotect(cuenta.Contrasena_Cuenta, out var contrasena))
                {
                    contrasenas[cuenta.Id_Cuenta] = contrasena;
                    continue;
                }

                var plataforma = cuenta.Plataforma?.Descripcion ?? $"Id plataforma {cuenta.Id_Plataforma}";
                return ServiceResult.Fail(
                    StatusCodes.Status409Conflict,
                    $"La contrasena de una cuenta de {plataforma} no se puede descifrar. Actualiza esa cuenta desde Registrar Productos antes de venderla.");
            }

            return null;
        }

        private string ObtenerContrasenaCuentaSegura(Cuentas cuenta)
        {
            return _cuentaPasswordProtector.TryUnprotect(cuenta.Contrasena_Cuenta, out var contrasena)
                ? contrasena
                : string.Empty;
        }
        private async Task<(bool Ok, ServiceResult Result, List<CompraLinea> Lineas)> ConstruirLineasCompra(List<DtoConfirmarCompraItem> items, int idTipoUsuario)
        {
            var lineas = new List<CompraLinea>();

            foreach (var item in items.Where(i => i.Cantidad > 0))
            {
                var tipo = NormalizarTipoProducto(item.Tipo_Producto);
                if (tipo == ProductoCombo)
                {
                    if (!item.Id_Combo.HasValue)
                    {
                        return (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "Debe seleccionar el combo."), lineas);
                    }

                    var combo = await _context.Combos
                        .AsNoTracking()
                        .Include(c => c.Plataformas)
                        .FirstOrDefaultAsync(c => c.Id_Combo == item.Id_Combo.Value && c.Id_Tipo_Usuario == idTipoUsuario && c.Vigente == 1);

                    if (combo == null)
                    {
                        return (false, ServiceResult.Fail(StatusCodes.Status404NotFound, "Uno de los combos no existe o esta inactivo."), lineas);
                    }

                    lineas.Add(new CompraLinea
                    {
                        TipoProducto = ProductoCombo,
                        IdCombo = combo.Id_Combo,
                        NombreProducto = combo.Nombre,
                        TiempoPantalla = combo.Tiempo_Pantalla,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = combo.Precio,
                        PlataformasRequeridas = combo.Plataformas.Select(p => new CompraPlataformaRequerida(p.Id_Plataforma, p.Cantidad)).ToList()
                    });
                    continue;
                }

                if (!item.Id_Plataforma.HasValue || item.Id_Plataforma.Value <= 0)
                {
                    return (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "Debe seleccionar la plataforma."), lineas);
                }

                var precio = await _context.PreciosProducto
                    .AsNoTracking()
                    .Include(p => p.Plataforma)
                    .FirstOrDefaultAsync(p => p.Id_Plataforma == item.Id_Plataforma.Value
                        && p.Id_Tipo_Usuario == idTipoUsuario
                        && p.Tiempo_Pantalla == item.Tiempo_Pantalla
                        && p.Vigente == 1);

                if (precio == null)
                {
                    return (false, ServiceResult.Fail(StatusCodes.Status404NotFound, "Uno de los productos no tiene precio activo."), lineas);
                }

                lineas.Add(new CompraLinea
                {
                    TipoProducto = ProductoPantalla,
                    IdPlataforma = item.Id_Plataforma.Value,
                    NombreProducto = precio.Plataforma?.Descripcion ?? $"Plataforma {item.Id_Plataforma.Value}",
                    TiempoPantalla = item.Tiempo_Pantalla,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = precio.Precio,
                    PlataformasRequeridas = new List<CompraPlataformaRequerida>
                    {
                        new(item.Id_Plataforma.Value, 1)
                    }
                });
            }

            return lineas.Count == 0
                ? (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "Debe seleccionar al menos un producto valido."), lineas)
                : (true, ServiceResult.Success(), lineas);
        }

        private async Task<(bool Ok, ServiceResult Result, string Nombre, string? Correo)> ObtenerComprador(DtoConfirmarCompraRequest model, string origen, int? idUsuario)
        {
            if (origen == "Publica")
            {
                var nombre = model.Nombre_Cliente?.Trim();
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    return (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El nombre del cliente es obligatorio."), string.Empty, null);
                }

                return (true, ServiceResult.Success(), nombre, NormalizarCorreo(model.Correo_Cliente));
            }

            if (!idUsuario.HasValue)
            {
                return (false, ServiceResult.Fail(StatusCodes.Status401Unauthorized, "No fue posible identificar el vendedor."), string.Empty, null);
            }

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id_Usuario == idUsuario.Value && u.Vigente == 1);

            if (usuario == null)
            {
                return (false, ServiceResult.Fail(StatusCodes.Status401Unauthorized, "No fue posible identificar el vendedor."), string.Empty, null);
            }

            return (true, ServiceResult.Success(), usuario.Nombre, NormalizarCorreo(usuario.E_Mail));
        }

        private async Task EnviarCorreoCompraSiAplica(string? correo, string nombre, string origen, DtoCompraResultado data)
        {
            if (string.IsNullOrWhiteSpace(correo))
            {
                return;
            }

            try
            {
                var textBody = ConstruirTextoCompra(nombre, origen, data);
                var htmlBody = ConstruirHtmlCompra(nombre, origen, data);
                await _emailSender.SendPurchaseConfirmationAsync(correo, "Compra confirmada", textBody, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible enviar el correo de compra a {Correo}", correo);
            }
        }

        private async Task EnviarCorreoCodigoSiAplica(CodigosCompra codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo.Correo_Cliente))
            {
                return;
            }

            try
            {
                var textBody = ConstruirTextoCodigoCompra(codigo);
                var htmlBody = ConstruirHtmlCodigoCompra(codigo);
                await _emailSender.SendPurchaseConfirmationAsync(codigo.Correo_Cliente, "Codigo de compra generado", textBody, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible enviar el codigo de compra a {Correo}", codigo.Correo_Cliente);
            }
        }

        private async Task EnviarCorreoRecargaBilleteraSiAplica(UsuarioEntity usuario, decimal valorRecargado, decimal saldoAnterior, decimal saldoNuevo, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(usuario.E_Mail))
            {
                return;
            }

            try
            {
                var textBody = ConstruirTextoRecargaBilletera(usuario, valorRecargado, saldoAnterior, saldoNuevo, descripcion);
                var htmlBody = ConstruirHtmlRecargaBilletera(usuario, valorRecargado, saldoAnterior, saldoNuevo, descripcion);
                await _emailSender.SendPurchaseConfirmationAsync(usuario.E_Mail, "Recarga de billetera", textBody, htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No fue posible enviar el correo de recarga de billetera a {Correo}", usuario.E_Mail);
            }
        }

        private static string ConstruirTextoCodigoCompra(CodigosCompra codigo)
        {
            return $"""
                {NombreEmpresa}
                Se ha generado un codigo para la compra de plataformas
                **Codigo:** {codigo.Codigo}
                **Saldo:** {codigo.Saldo_Disponible:C0}
                **Nombre:** {codigo.Nombre_Cliente}
                **Correo:** {codigo.Correo_Cliente}
                Guarde esta informacion mientras tenga saldo vigente para comprar y para consultar su historial de compras
                """;
        }

        private static string ConstruirTextoRecargaBilletera(UsuarioEntity usuario, decimal valorRecargado, decimal saldoAnterior, decimal saldoNuevo, string? descripcion)
        {
            var lineas = new List<string>
            {
                NombreEmpresa,
                "Se ha realizado una recarga en tu billetera de vendedor.",
                string.Empty,
                $"**Vendedor:** {usuario.Nombre}",
                $"**Usuario:** {usuario.Usuario}",
                $"**Valor recargado:** {valorRecargado:C0}",
                $"**Saldo anterior:** {saldoAnterior:C0}",
                $"**Saldo actual:** {saldoNuevo:C0}"
            };

            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                lineas.Add($"**Descripcion:** {descripcion.Trim()}");
            }

            return string.Join(Environment.NewLine, lineas);
        }

        private static string ConstruirHtmlCodigoCompra(CodigosCompra codigo)
        {
            return $"""
                <h2>{Html(NombreEmpresa)}</h2>
                <p>Se ha generado un codigo para la compra de plataformas</p>
                <p><strong>Codigo:</strong> {Html(codigo.Codigo)}</p>
                <p><strong>Saldo:</strong> {codigo.Saldo_Disponible:C0}</p>
                <p><strong>Nombre:</strong> {Html(codigo.Nombre_Cliente)}</p>
                <p><strong>Correo:</strong> {Html(codigo.Correo_Cliente)}</p>
                <p>Guarde esta informacion mientras tenga saldo vigente para comprar y para consultar su historial de compras</p>
                """;
        }

        private static string ConstruirHtmlRecargaBilletera(UsuarioEntity usuario, decimal valorRecargado, decimal saldoAnterior, decimal saldoNuevo, string? descripcion)
        {
            var descripcionHtml = string.IsNullOrWhiteSpace(descripcion)
                ? string.Empty
                : $"<p><strong>Descripcion:</strong> {Html(descripcion.Trim())}</p>";

            return $"""
                <h2>{Html(NombreEmpresa)}</h2>
                <p>Se ha realizado una recarga en tu billetera de vendedor.</p>
                <p><strong>Vendedor:</strong> {Html(usuario.Nombre)}</p>
                <p><strong>Usuario:</strong> {Html(usuario.Usuario)}</p>
                <p><strong>Valor recargado:</strong> {valorRecargado:C0}</p>
                <p><strong>Saldo anterior:</strong> {saldoAnterior:C0}</p>
                <p><strong>Saldo actual:</strong> {saldoNuevo:C0}</p>
                {descripcionHtml}
                """;
        }

        private static string ConstruirTextoCompra(string nombre, string origen, DtoCompraResultado data)
        {
            var lineas = new List<string>
            {
                "**Compra Confirmada**",
                string.Empty,
                $"Hola {nombre}, gracias por tu compra.",
                string.Empty
            };

            foreach (var cuenta in data.Cuentas)
            {
                lineas.Add($"*# Pedido:* {data.Id_Pedido}");
                lineas.Add($"*Cuenta:* {cuenta.Plataforma}");
                lineas.Add($"*Correo:* {cuenta.Correo_Cuenta}");
                lineas.Add($"*Contraseña:* {cuenta.Contrasena_Cuenta}");
                lineas.Add($"*Perfil:* {cuenta.Perfil_Cuenta ?? string.Empty}");
                lineas.Add($"*Pin:* {cuenta.Pin_Cuenta ?? string.Empty}");
                lineas.Add($"*Fecha Vencimiento:* {cuenta.Fecha_Vencimiento:yyyy-MM-dd}");
                lineas.Add($"*Total Pagado:* {data.Total:C0}");
                lineas.Add(string.Empty);
            }

            if (origen == "Interna")
            {
                lineas.Add($"*Saldo restante en billetera:* {data.Saldo_Restante:C0}");
                lineas.Add("*Detalle de la compra:*");
                lineas.AddRange(data.Detalles.Select(d => $"{d.Producto} | {d.Cantidad} | {d.Valor_Unitario:C0} | {d.Subtotal:C0}"));
            }

            return string.Join(Environment.NewLine, lineas);
        }

        private static string ConstruirHtmlCompra(string nombre, string origen, DtoCompraResultado data)
        {
            var cuentaHtml = string.Join("", data.Cuentas.Select(c => $"""
                <div style="margin:0 0 18px 0;padding:14px;border:1px solid #d7e3f4;border-radius:8px;">
                    <p><strong># Pedido:</strong> {data.Id_Pedido}</p>
                    <p><strong>Cuenta: {Html(c.Plataforma)}</strong></p>
                    <p><strong>Correo:</strong> {Html(c.Correo_Cuenta)}</p>
                    <p><strong>Contraseña:</strong> {Html(c.Contrasena_Cuenta)}</p>
                    <p><strong>Perfil:</strong> {Html(c.Perfil_Cuenta)}</p>
                    <p><strong>Pin:</strong> {Html(c.Pin_Cuenta)}</p>
                    <p><strong>Fecha Vencimiento:</strong> {c.Fecha_Vencimiento:yyyy-MM-dd}</p>
                    <p><strong>Total Pagado:</strong> {data.Total:C0}</p>
                </div>
                """));

            var detalleVendedor = origen == "Interna"
                ? $"""
                    <p><strong>Saldo restante en billetera:</strong> {data.Saldo_Restante:C0}</p>
                    <p><strong>Detalle de la compra:</strong></p>
                    <ul>{string.Join("", data.Detalles.Select(d => $"<li>{Html(d.Producto)} | {d.Cantidad} | {d.Valor_Unitario:C0} | {d.Subtotal:C0}</li>"))}</ul>
                    """
                : string.Empty;

            return $"""
                <h2>Compra Confirmada</h2>
                <p>Hola {Html(nombre)}, gracias por tu compra.</p>
                {cuentaHtml}
                {detalleVendedor}
                """;
        }

        private static string? NormalizarCorreo(string? correo)
        {
            var value = correo?.Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string Html(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private async Task<List<Cuentas>> TomarCuentasDisponibles(CompraLinea linea, int idTipoUsuario)
        {
            var cuentas = new List<Cuentas>();
            foreach (var requerida in linea.PlataformasRequeridas)
            {
                var cantidad = requerida.Cantidad * linea.Cantidad;
                var cuentasPlataforma = await _context.Cuentas
                    .Include(c => c.Plataforma)
                    .Where(c => c.Vigente == 1
                        && c.Id_Tipo_Usuario == idTipoUsuario
                        && c.Id_Plataforma == requerida.IdPlataforma
                        && c.Tiempo_Pantalla == linea.TiempoPantalla)
                    .OrderBy(c => c.Fecha_Creacion)
                    .Take(cantidad)
                    .ToListAsync();

                cuentas.AddRange(cuentasPlataforma);
            }

            return cuentas;
        }

        private async Task<Dictionary<int, int>> CalcularDisponibilidadCombos(List<Combos> combos)
        {
            var idsPlataforma = combos.SelectMany(c => c.Plataformas.Select(p => p.Id_Plataforma)).Distinct().ToList();
            var tiempos = combos.Select(c => c.Tiempo_Pantalla).Distinct().ToList();
            var tiposUsuario = combos.Select(c => c.Id_Tipo_Usuario).Distinct().ToList();

            var stock = await _context.Cuentas
                .AsNoTracking()
                .Where(c => c.Vigente == 1
                    && idsPlataforma.Contains(c.Id_Plataforma)
                    && tiempos.Contains(c.Tiempo_Pantalla)
                    && tiposUsuario.Contains(c.Id_Tipo_Usuario))
                .GroupBy(c => new { c.Id_Plataforma, c.Tiempo_Pantalla, c.Id_Tipo_Usuario })
                .Select(g => new
                {
                    g.Key.Id_Plataforma,
                    g.Key.Tiempo_Pantalla,
                    g.Key.Id_Tipo_Usuario,
                    Cantidad = g.Count()
                })
                .ToListAsync();

            return combos.ToDictionary(
                c => c.Id_Combo,
                c => c.Plataformas.Any()
                    ? c.Plataformas.Min(p =>
                    {
                        var disponibles = stock
                            .Where(s => s.Id_Plataforma == p.Id_Plataforma
                                && s.Tiempo_Pantalla == c.Tiempo_Pantalla
                                && s.Id_Tipo_Usuario == c.Id_Tipo_Usuario)
                            .Sum(s => s.Cantidad);
                        return disponibles / Math.Max(1, p.Cantidad);
                    })
                    : 0);
        }

        private IQueryable<Cuentas> QueryCuentas()
        {
            return QueryCuentasInventario();
        }

        private IQueryable<Cuentas> QueryCuentasInventario()
        {
            return _context.Cuentas
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(c => c.Plataforma)
                .Include(c => c.TipoUsuario);
        }

        private IQueryable<Pedidos> QueryDetallePedido()
        {
            return _context.Pedidos
                .AsNoTracking()
                .Include(p => p.CodigoCompra)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Plataforma)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Combo)
                .Include(p => p.Cuentas)
                    .ThenInclude(pc => pc.Cuenta!)
                        .ThenInclude(c => c.Plataforma);
        }

        private DtoDetalleCompraItem MapDetalleCompra(Pedidos pedido)
        {
            return new DtoDetalleCompraItem
            {
                Id_Pedido = pedido.Id_Pedido,
                Total = pedido.Total,
                Saldo_Restante = pedido.CodigoCompra?.Saldo_Disponible ?? 0m,
                Detalles = pedido.Detalles
                    .OrderBy(d => d.Id_Pedido_Detalle)
                    .Select(d => new DtoCompraDetalleResultado
                    {
                        Producto = ObtenerNombreDetalle(d),
                        Cantidad = d.Cantidad,
                        Valor_Unitario = d.Precio_Unitario,
                        Subtotal = d.Subtotal
                    })
                    .ToList(),
                Cuentas = pedido.Cuentas
                    .Where(pc => pc.Cuenta != null)
                    .OrderBy(pc => pc.Cuenta!.Plataforma!.Descripcion)
                    .ThenBy(pc => pc.Id_Cuenta)
                    .Select(pc => new DtoCompraCuentaItem
                    {
                        Id_Cuenta = pc.Id_Cuenta,
                        Id_Plataforma = pc.Cuenta!.Id_Plataforma,
                        Plataforma = pc.Cuenta.Plataforma?.Descripcion ?? string.Empty,
                        Correo_Cuenta = pc.Cuenta.Correo_Cuenta,
                        Contrasena_Cuenta = ObtenerContrasenaCuentaSegura(pc.Cuenta),
                        Perfil_Cuenta = pc.Cuenta.Perfil_Cuenta,
                        Pin_Cuenta = pc.Cuenta.Pin_Cuenta,
                        Fecha_Compra = pedido.Fecha_Compra,
                        Fecha_Vencimiento = pc.Cuenta.Fecha_Vencimiento,
                        Tiempo_Pantalla = pc.Cuenta.Tiempo_Pantalla
                    })
                    .ToList()
            };
        }

        private static DtoHistorialCompraItem MapHistorial(Pedidos pedido)
        {
            return new DtoHistorialCompraItem
            {
                Id_Pedido = pedido.Id_Pedido,
                Origen = pedido.Origen,
                TipoUsuario = pedido.TipoUsuario?.Descripcion ?? string.Empty,
                Usuario = pedido.Usuario != null ? pedido.Usuario.Usuario : null,
                Codigo = pedido.CodigoCompra?.Codigo,
                Nombre_Cliente = pedido.Nombre_Cliente,
                Correo_Cliente = pedido.Correo_Cliente,
                Plataforma = string.Join(", ", pedido.Detalles
                    .OrderBy(d => d.Id_Pedido_Detalle)
                    .Select(ObtenerNombreDetalle)
                    .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                    .Distinct()),
                Total = pedido.Total,
                Fecha_Compra = pedido.Fecha_Compra,
                CantidadCuentas = pedido.Cuentas.Count
            };
        }

        private static string ObtenerNombreDetalle(PedidoDetalles detalle)
        {
            return detalle.Tipo_Producto == ProductoCombo
                ? detalle.Combo?.Nombre ?? string.Empty
                : detalle.Plataforma?.Descripcion ?? string.Empty;
        }

        private DtoCuentaItem MapCuenta(Cuentas cuenta, bool incluirContrasena)
        {
            return new DtoCuentaItem
            {
                Id_Cuenta = cuenta.Id_Cuenta,
                Id_Plataforma = cuenta.Id_Plataforma,
                Plataforma = cuenta.Plataforma?.Descripcion ?? string.Empty,
                Id_Tipo_Usuario = cuenta.Id_Tipo_Usuario,
                TipoUsuario = cuenta.TipoUsuario?.Descripcion ?? string.Empty,
                Tiempo_Pantalla = cuenta.Tiempo_Pantalla,
                Correo_Cuenta = cuenta.Correo_Cuenta,
                Contrasena_Cuenta = incluirContrasena ? ObtenerContrasenaCuentaSegura(cuenta) : string.Empty,
                Perfil_Cuenta = cuenta.Perfil_Cuenta,
                Pin_Cuenta = cuenta.Pin_Cuenta,
                Fecha_Vencimiento = cuenta.Fecha_Vencimiento,
                Vigente = cuenta.Vigente
            };
        }

        private static DtoComboItem MapCombo(Combos combo)
        {
            return new DtoComboItem
            {
                Id_Combo = combo.Id_Combo,
                Nombre = combo.Nombre,
                Descripcion = combo.Descripcion,
                ImagenUrl = combo.ImagenUrl,
                Id_Tipo_Usuario = combo.Id_Tipo_Usuario,
                TipoUsuario = combo.TipoUsuario?.Descripcion ?? string.Empty,
                Tiempo_Pantalla = combo.Tiempo_Pantalla,
                Precio = combo.Precio,
                Orden = combo.Orden,
                Vigente = combo.Vigente,
                Plataformas = combo.Plataformas
                    .OrderBy(p => p.Plataforma!.Descripcion)
                    .Select(p => new DtoComboPlataformaItem
                    {
                        Id_Plataforma = p.Id_Plataforma,
                        Plataforma = p.Plataforma?.Descripcion ?? string.Empty,
                        Cantidad = p.Cantidad
                    })
                    .ToList()
            };
        }

        private async Task<(bool Ok, ServiceResult Result, Combos? Combo)> CrearComboDesdeRequest(DtoComboRequest model, AuditContext audit, int? idCombo = null)
        {
            if (model.Plataformas == null || !model.Plataformas.Any())
            {
                return (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "Debe agregar al menos una plataforma al combo."), null);
            }

            var tipoValido = await _context.Dominios.AnyAsync(d => d.Id_Dominio == model.Id_Tipo_Usuario && d.Id_Padre == DominioTipoUsuario && d.Vigente == 1);
            if (!tipoValido)
            {
                return (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El tipo de usuario no existe o esta inactivo."), null);
            }

            var idsPlataforma = model.Plataformas.Select(p => p.Id_Plataforma).Distinct().ToList();
            var plataformasValidas = await _context.Dominios.CountAsync(d => idsPlataforma.Contains(d.Id_Dominio) && d.Id_Padre == DominioPlataformas && d.Vigente == 1);
            if (plataformasValidas != idsPlataforma.Count)
            {
                return (false, ServiceResult.Fail(StatusCodes.Status400BadRequest, "Una o mas plataformas no existen o estan inactivas."), null);
            }

            var nombre = model.Nombre.Trim();
            var existe = await _context.Combos.AnyAsync(c =>
                (!idCombo.HasValue || c.Id_Combo != idCombo.Value) &&
                c.Nombre.Trim().ToLower() == nombre.ToLower() &&
                c.Id_Tipo_Usuario == model.Id_Tipo_Usuario &&
                c.Tiempo_Pantalla == model.Tiempo_Pantalla);

            if (existe)
            {
                return (false, ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un combo con ese nombre, tipo de usuario y dias."), null);
            }

            var combo = new Combos
            {
                Nombre = nombre,
                Descripcion = model.Descripcion?.Trim(),
                ImagenUrl = model.ImagenUrl?.Trim(),
                Id_Tipo_Usuario = model.Id_Tipo_Usuario,
                Tiempo_Pantalla = model.Tiempo_Pantalla,
                Precio = decimal.Round(model.Precio, 2),
                Orden = model.Orden,
                Vigente = model.Vigente,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine,
                Plataformas = model.Plataformas
                    .GroupBy(p => p.Id_Plataforma)
                    .Select(g => new ComboPlataformas
                    {
                        Id_Plataforma = g.Key,
                        Cantidad = g.Sum(p => p.Cantidad)
                    })
                    .ToList()
            };

            return (true, ServiceResult.Success(), combo);
        }

        private async Task<ServiceResult?> ValidarDominios(int idPlataforma, int idTipoUsuario)
        {
            var plataformaValida = await _context.Dominios.AnyAsync(d => d.Id_Dominio == idPlataforma && d.Id_Padre == DominioPlataformas && d.Vigente == 1);
            if (!plataformaValida)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "La plataforma no existe o esta inactiva.");
            }

            var tipoValido = await _context.Dominios.AnyAsync(d => d.Id_Dominio == idTipoUsuario && d.Id_Padre == DominioTipoUsuario && d.Vigente == 1);
            return tipoValido
                ? null
                : ServiceResult.Fail(StatusCodes.Status400BadRequest, "El tipo de usuario no existe o esta inactivo.");
        }

        private async Task<CodigosCompra?> ObtenerCodigoValido(string codigo, string? correo)
        {
            var codigoCompra = await ObtenerCodigoPorCorreo(codigo, correo);
            return codigoCompra != null && CodigoTieneSaldo(codigoCompra)
                ? codigoCompra
                : null;
        }

        private async Task<CodigosCompra?> ObtenerCodigoPorCorreo(string codigo, string? correo)
        {
            var codigoNormalizado = codigo.Trim().ToUpperInvariant();
            var correoNormalizado = NormalizarCorreo(correo);
            if (string.IsNullOrWhiteSpace(correoNormalizado))
            {
                return null;
            }

            return await _context.CodigosCompra
                .FirstOrDefaultAsync(c => c.Codigo == codigoNormalizado
                    && c.Correo_Cliente.ToLower() == correoNormalizado.ToLower());
        }

        private static bool CodigoTieneSaldo(CodigosCompra codigo)
        {
            return codigo.Vigente == 1
                && codigo.Saldo_Disponible > 0
                && (!codigo.Fecha_Expiracion.HasValue || codigo.Fecha_Expiracion.Value >= DateTime.UtcNow);
        }

        private async Task<string> GenerarCodigoUnico()
        {
            for (var i = 0; i < 20; i++)
            {
                var codigo = $"CS-{Convert.ToHexString(RandomNumberGenerator.GetBytes(5))}";
                if (!await _context.CodigosCompra.AnyAsync(c => c.Codigo == codigo))
                {
                    return codigo;
                }
            }

            throw new InvalidOperationException("No fue posible generar un codigo de compra unico.");
        }

        private static DtoCodigoCompraItem MapCodigo(CodigosCompra codigo)
        {
            return new DtoCodigoCompraItem
            {
                Id_Codigo_Compra = codigo.Id_Codigo_Compra,
                Codigo = codigo.Codigo,
                Nombre_Cliente = codigo.Nombre_Cliente,
                Correo_Cliente = codigo.Correo_Cliente,
                Valor_Inicial = codigo.Valor_Inicial,
                Saldo_Disponible = codigo.Saldo_Disponible,
                Fecha_Expiracion = codigo.Fecha_Expiracion,
                Vigente = codigo.Vigente
            };
        }

        private static string NormalizarTipoProducto(string? tipoProducto)
        {
            return string.Equals(tipoProducto, ProductoCombo, StringComparison.OrdinalIgnoreCase)
                ? ProductoCombo
                : ProductoPantalla;
        }

        private static DateTime NormalizarFechaPedido(DateTime fechaPedido)
        {
            if (fechaPedido == default)
            {
                return DateTime.UtcNow;
            }

            if (fechaPedido.Kind == DateTimeKind.Utc)
            {
                return fechaPedido;
            }

            return ConvertirLocalAUtc(fechaPedido);
        }

        private static DateTime CalcularFechaVencimiento(DateTime fechaPedido, int tiempoPantalla)
        {
            var fechaLocal = ConvertirUtcALocal(fechaPedido);
            var inicioVigenciaLocal = fechaLocal.Hour >= 20
                ? fechaLocal.Date.AddDays(1)
                : fechaLocal.Date;
            var diasVigencia = Math.Max(1, tiempoPantalla);
            var vencimientoLocal = inicioVigenciaLocal.AddDays(diasVigencia);
            return ConvertirLocalAUtc(vencimientoLocal);
        }

        private static DateTime ConvertirLocalAUtc(DateTime fechaLocal)
        {
            var sinKind = DateTime.SpecifyKind(fechaLocal, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(sinKind, ObtenerZonaColombia());
        }

        private static DateTime ConvertirUtcALocal(DateTime fechaUtc)
        {
            var utc = fechaUtc.Kind == DateTimeKind.Utc
                ? fechaUtc
                : DateTime.SpecifyKind(fechaUtc, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utc, ObtenerZonaColombia());
        }

        private static TimeZoneInfo ObtenerZonaColombia()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
        }

        private sealed class CompraLinea
        {
            public string TipoProducto { get; init; } = ProductoPantalla;
            public string NombreProducto { get; init; } = string.Empty;
            public int? IdPlataforma { get; init; }
            public int? IdCombo { get; init; }
            public int TiempoPantalla { get; init; }
            public int Cantidad { get; init; }
            public decimal PrecioUnitario { get; init; }
            public decimal Subtotal => PrecioUnitario * Cantidad;
            public int TotalCuentasRequeridas => PlataformasRequeridas.Sum(p => p.Cantidad * Cantidad);
            public List<CompraPlataformaRequerida> PlataformasRequeridas { get; init; } = new();
        }

        private sealed record CompraPlataformaRequerida(int IdPlataforma, int Cantidad);
    }
}





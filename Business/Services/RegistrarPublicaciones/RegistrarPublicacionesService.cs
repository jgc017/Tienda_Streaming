using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.RegistrarPublicaciones;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.RegistrarPublicaciones;

namespace Tienda_Streaming.Business.Services.RegistrarPublicaciones
{
    // Servicio que administra el contenido visible en el inicio publico y sus paginas.
    public class RegistrarPublicacionesService : IRegistrarPublicaciones
    {
        private const int DominioTipoContenidoInicio = 26;
        private const string TipoContenidoSlider = "Slider";
        private const string TipoContenidoContacto = "Contacto";
        private const string DominioRaiz = "SIN DATOS";
        private readonly AppDbContext _context;
        private readonly ILogger<RegistrarPublicacionesService> _logger;

        public RegistrarPublicacionesService(AppDbContext context, ILogger<RegistrarPublicacionesService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // P_InsInicioContenido: registra un contenido publico administrable.
        public async Task<ServiceResult> P_InsInicioContenido(DtoInicioContenidoCreateRequest model, AuditContext audit)
        {
            var tipoContenido = await ObtenerTipoContenidoValido(model.IdTipoContenido);
            if (tipoContenido.Error != null)
            {
                return tipoContenido.Error;
            }

            var contenido = new InicioContenido
            {
                TipoContenido = tipoContenido.Dominio!.Descripcion.Trim(),
                Titulo = model.Titulo.Trim(),
                Resumen = model.Resumen?.Trim(),
                Contenido = model.Contenido?.Trim(),
                ImagenUrl = NormalizarUrlLocal(model.ImagenUrl),
                EnlaceUrl = model.EnlaceUrl?.Trim(),
                TextoBoton = model.TextoBoton?.Trim(),
                MostrarEnInicio = model.MostrarEnInicio,
                Orden = model.Orden,
                Vigente = 1,
                Id_Usuario_Creacion = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            try
            {
                await AplicarReglasDeInicioYOrden(contenido, null, audit);
                _context.InicioContenidos.Add(contenido);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error registrando contenido de inicio {TipoContenido}", model.TipoContenido);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "No fue posible registrar el contenido.");
            }

            return ServiceResult.Success(
                "Contenido registrado correctamente.",
                auditDescription: $"Registro de contenido {contenido.TipoContenido} {contenido.Titulo} con id {contenido.Id_InicioContenido}");
        }

        // F_GetInicioContenidosList: consulta todos los registros para la grilla administrativa.
        public async Task<ServiceResult> F_GetInicioContenidosList()
        {
            var contenidos = await QueryBase()
                .OrderBy(i => i.TipoContenido)
                .ThenBy(i => i.Orden)
                .ThenByDescending(i => i.Fecha_Creacion)
                .ToListAsync();

            return ServiceResult.Success(data: contenidos.Select(MapItem).ToList());
        }

        // F_GetInicioContenido: consulta un registro por id para editarlo.
        public async Task<ServiceResult> F_GetInicioContenido(int idInicioContenido)
        {
            var contenido = await QueryBase()
                .FirstOrDefaultAsync(i => i.Id_InicioContenido == idInicioContenido);

            if (contenido == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Contenido no encontrado.");
            }

            return ServiceResult.Success(
                data: MapItem(contenido),
                auditDescription: $"Consulta de contenido {contenido.TipoContenido} {contenido.Titulo} con id {contenido.Id_InicioContenido}");
        }

        // P_UdpInicioContenido: actualiza datos, orden, visibilidad en inicio y estado.
        public async Task<ServiceResult> P_UdpInicioContenido(int idInicioContenido, DtoInicioContenidoUpdateRequest model, AuditContext audit)
        {
            var tipoContenido = await ObtenerTipoContenidoValido(model.IdTipoContenido);
            if (tipoContenido.Error != null)
            {
                return tipoContenido.Error;
            }

            var contenido = await _context.InicioContenidos.FindAsync(idInicioContenido);
            if (contenido == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Contenido no existe.");
            }

            contenido.TipoContenido = tipoContenido.Dominio!.Descripcion.Trim();
            contenido.Titulo = model.Titulo.Trim();
            contenido.Resumen = model.Resumen?.Trim();
            contenido.Contenido = model.Contenido?.Trim();
            contenido.ImagenUrl = NormalizarUrlLocal(model.ImagenUrl);
            contenido.EnlaceUrl = model.EnlaceUrl?.Trim();
            contenido.TextoBoton = model.TextoBoton?.Trim();
            contenido.MostrarEnInicio = model.MostrarEnInicio;
            contenido.Orden = model.Orden;
            contenido.Vigente = model.Vigente;
            contenido.Id_Usuario_Modifica = audit.UserId;
            contenido.Fecha_Modifica = DateTime.UtcNow;
            contenido.Maquina_Modifica = audit.Machine;

            await AplicarReglasDeInicioYOrden(contenido, idInicioContenido, audit);
            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Contenido actualizado correctamente.",
                auditDescription: $"Actualizacion de contenido {contenido.TipoContenido} {contenido.Titulo} con id {contenido.Id_InicioContenido}");
        }

        // P_DeleteInicioContenido: realiza baja logica del contenido.
        public async Task<ServiceResult> P_DeleteInicioContenido(int idInicioContenido, AuditContext audit)
        {
            var contenido = await _context.InicioContenidos.FindAsync(idInicioContenido);
            if (contenido == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Contenido no existe.");
            }

            if (contenido.Vigente == 0)
            {
                return ServiceResult.Success(
                    "El contenido ya se encontraba inactivo.",
                    auditDescription: $"Eliminacion logica de contenido {contenido.TipoContenido} {contenido.Titulo} con id {contenido.Id_InicioContenido}");
            }

            contenido.Vigente = 0;
            contenido.Id_Usuario_Modifica = audit.UserId;
            contenido.Fecha_Modifica = DateTime.UtcNow;
            contenido.Maquina_Modifica = audit.Machine;

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Contenido marcado como inactivo correctamente.",
                auditDescription: $"Eliminacion logica de contenido {contenido.TipoContenido} {contenido.Titulo} con id {contenido.Id_InicioContenido}");
        }

        // F_GetInicioPublico: agrupa los contenidos visibles para Home/VwIndex.
        public async Task<DtoInicioPublico> F_GetInicioPublico()
        {
            var visibles = await ContenidosPublicos()
                .ToListAsync();

            return new DtoInicioPublico
            {
                Slider = visibles
                    .Where(i => i.TipoContenido == DtoInicioContenidoTipos.Slider && i.MostrarEnInicio == 1)
                    .OrderBy(i => i.Orden)
                    .Select(MapItem)
                    .ToList(),
                Contacto = visibles
                    .Where(i => i.TipoContenido == DtoInicioContenidoTipos.Contacto && i.MostrarEnInicio == 1)
                    .OrderBy(i => i.Orden)
                    .Select(MapItem)
                    .FirstOrDefault()
            };
        }

        // F_GetContenidoPublicoPorTipo: alimenta las paginas publicas por seccion.
        public async Task<List<DtoInicioContenidoItem>> F_GetContenidoPublicoPorTipo(string tipoContenido)
        {
            var contenidos = await ContenidosPublicos()
                .Where(i => i.TipoContenido == tipoContenido)
                .OrderBy(i => i.Orden)
                .ThenByDescending(i => i.Fecha_Creacion)
                .ToListAsync();

            return contenidos.Select(MapItem).ToList();
        }

        // F_GetContenidoPublicoDetalle: trae una publicacion/noticia por id para enlace directo.
        public async Task<DtoInicioContenidoItem?> F_GetContenidoPublicoDetalle(int idInicioContenido)
        {
            var contenido = await ContenidosPublicos()
                .FirstOrDefaultAsync(i => i.Id_InicioContenido == idInicioContenido);

            return contenido == null ? null : MapItem(contenido);
        }

        private IQueryable<InicioContenido> QueryBase()
        {
            return _context.InicioContenidos.AsNoTracking();
        }

        private IQueryable<InicioContenido> ContenidosPublicos()
        {
            return QueryBase().Where(i => i.Vigente == 1);
        }

        private static DtoInicioContenidoItem MapItem(InicioContenido contenido)
        {
            return new DtoInicioContenidoItem
            {
                Id_InicioContenido = contenido.Id_InicioContenido,
                TipoContenido = contenido.TipoContenido,
                Titulo = contenido.Titulo,
                Resumen = contenido.Resumen,
                Contenido = contenido.Contenido,
                ImagenUrl = contenido.ImagenUrl,
                EnlaceUrl = contenido.EnlaceUrl,
                TextoBoton = contenido.TextoBoton,
                MostrarEnInicio = contenido.MostrarEnInicio,
                Orden = contenido.Orden,
                Vigente = contenido.Vigente,
                Fecha_Creacion = contenido.Fecha_Creacion
            };
        }

        private async Task<(Models.Administracion.Dominios? Dominio, ServiceResult? Error)> ObtenerTipoContenidoValido(int idTipoContenido)
        {
            if (idTipoContenido <= 0)
            {
                return (null, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El tipo de contenido no es valido."));
            }

            var dominio = await _context.Dominios
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id_Dominio == idTipoContenido
                    && d.Vigente == 1
                    && d.Descripcion.ToUpper() != DominioRaiz
                    && d.Id_Padre == DominioTipoContenidoInicio);

            if (dominio != null && !EsTipoPermitido(dominio.Descripcion))
            {
                dominio = null;
            }

            return dominio == null
                ? (null, ServiceResult.Fail(StatusCodes.Status400BadRequest, "El tipo de contenido no existe en dominios o esta inactivo."))
                : (dominio, null);
        }

        // AplicarReglasDeInicioYOrden: evita duplicidad de orden por tipo y
        // controla que solo exista un contenido visible en inicio por seccion
        // que no sea Slider. Slider permite multiples banners en el carrusel.
        private async Task AplicarReglasDeInicioYOrden(InicioContenido contenido, int? idContenidoActual, AuditContext audit)
        {
            if (contenido.Vigente != 1)
            {
                contenido.MostrarEnInicio = 0;
            }

            contenido.Orden = await ObtenerOrdenNormalizado(contenido.TipoContenido, contenido.Orden, idContenidoActual);

            await ReubicarOrdenesDuplicadas(contenido.TipoContenido, contenido.Orden, idContenidoActual, audit);

            if (contenido.MostrarEnInicio == 1 && !PermiteMultiplesEnInicio(contenido.TipoContenido))
            {
                await DesmarcarOtrosContenidosInicio(contenido.TipoContenido, idContenidoActual, audit);
            }
        }

        // Si el usuario no define un orden valido, se asigna el siguiente
        // disponible dentro del mismo tipo de contenido.
        private async Task<int> ObtenerOrdenNormalizado(string tipoContenido, int ordenSolicitado, int? idContenidoActual)
        {
            if (ordenSolicitado > 0)
            {
                return ordenSolicitado;
            }

            var query = _context.InicioContenidos
                .Where(i => i.Vigente == 1 && i.TipoContenido == tipoContenido);

            if (idContenidoActual.HasValue)
            {
                query = query.Where(i => i.Id_InicioContenido != idContenidoActual.Value);
            }

            var ultimoOrden = await query
                .Select(i => (int?)i.Orden)
                .MaxAsync() ?? 0;

            return ultimoOrden + 1;
        }

        // Cuando dos registros del mismo tipo intentan usar el mismo orden,
        // mueve los existentes a la siguiente posicion disponible.
        private async Task ReubicarOrdenesDuplicadas(string tipoContenido, int ordenSolicitado, int? idContenidoActual, AuditContext audit)
        {
            var query = _context.InicioContenidos
                .Where(i => i.Vigente == 1
                    && i.TipoContenido == tipoContenido
                    && i.Orden >= ordenSolicitado);

            if (idContenidoActual.HasValue)
            {
                query = query.Where(i => i.Id_InicioContenido != idContenidoActual.Value);
            }

            var registros = await query
                .OrderBy(i => i.Orden)
                .ThenBy(i => i.Fecha_Creacion)
                .ToListAsync();

            var siguienteOrden = ordenSolicitado + 1;
            foreach (var registro in registros)
            {
                if (registro.Orden < siguienteOrden)
                {
                    registro.Orden = siguienteOrden;
                    registro.Id_Usuario_Modifica = audit.UserId;
                    registro.Fecha_Modifica = DateTime.UtcNow;
                    registro.Maquina_Modifica = audit.Machine;
                }

                siguienteOrden = registro.Orden + 1;
            }
        }

        // Contacto usa un solo contenido visible en inicio; Slider permite multiples banners.
        private async Task DesmarcarOtrosContenidosInicio(string tipoContenido, int? idContenidoActual, AuditContext audit)
        {
            var query = _context.InicioContenidos
                .Where(i => i.Vigente == 1
                    && i.TipoContenido == tipoContenido
                    && i.MostrarEnInicio == 1);

            if (idContenidoActual.HasValue)
            {
                query = query.Where(i => i.Id_InicioContenido != idContenidoActual.Value);
            }

            var registros = await query.ToListAsync();
            foreach (var registro in registros)
            {
                registro.MostrarEnInicio = 0;
                registro.Id_Usuario_Modifica = audit.UserId;
                registro.Fecha_Modifica = DateTime.UtcNow;
                registro.Maquina_Modifica = audit.Machine;
            }
        }

        private static bool PermiteMultiplesEnInicio(string tipoContenido)
        {
            return string.Equals(tipoContenido, TipoContenidoSlider, StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsTipoPermitido(string tipoContenido)
        {
            return string.Equals(tipoContenido, TipoContenidoSlider, StringComparison.OrdinalIgnoreCase)
                || string.Equals(tipoContenido, TipoContenidoContacto, StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizarUrlLocal(string? url)
        {
            var limpia = url?.Trim();
            if (string.IsNullOrWhiteSpace(limpia))
            {
                return null;
            }

            return limpia.Replace("\\", "/");
        }
    }
}



using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.ImagenesProducto;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.ImagenesProducto;

namespace Tienda_Streaming.Business.Services.ImagenesProducto
{
    public class ImagenesProductoService : IImagenesProducto
    {
        private const int DominioPlataformas = 10;
        private const int DominioTipoImagen = 34;
        private const int TipoPantallaIndividual = 35;
        private readonly AppDbContext _context;
        private readonly ILogger<ImagenesProductoService> _logger;

        public ImagenesProductoService(AppDbContext context, ILogger<ImagenesProductoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult> P_InsImagenProducto(DtoImagenProductoCreateRequest model, AuditContext audit)
        {
            var validacion = await ValidarPlataforma(model.Id_Plataforma);
            if (validacion != null)
            {
                return validacion;
            }

            validacion = await ValidarTipoImagen(model.Id_Tipo_Imagen);
            if (validacion != null)
            {
                return validacion;
            }

            var imagen = await _context.ImagenesProducto
                .FirstOrDefaultAsync(i => i.Id_Plataforma == model.Id_Plataforma
                    && i.Id_Tipo_Imagen == model.Id_Tipo_Imagen);

            var esNuevo = imagen == null;
            var estabaActiva = imagen?.Vigente == 1;
            if (imagen == null)
            {
                imagen = new Models.Administracion.ImagenesProducto
                {
                    Id_Usuario_Creacion = audit.UserId,
                    Fecha_Creacion = DateTime.UtcNow,
                    Maquina_Creacion = audit.Machine
                };

                _context.ImagenesProducto.Add(imagen);
            }

            imagen.Id_Plataforma = model.Id_Plataforma;
            imagen.Id_Tipo_Imagen = model.Id_Tipo_Imagen;
            imagen.ImagenUrl = NormalizarUrlLocal(model.ImagenUrl);
            imagen.Descripcion = model.Descripcion?.Trim();
            imagen.Vigente = model.Vigente;

            if (!esNuevo)
            {
                imagen.Id_Usuario_Modifica = audit.UserId;
                imagen.Fecha_Modifica = DateTime.UtcNow;
                imagen.Maquina_Modifica = audit.Machine;
            }

            try
            {
                if (esNuevo || imagen.Orden <= 0 || (!estabaActiva && imagen.Vigente == 1))
                {
                    imagen.Orden = await ObtenerSiguienteOrden(model.Id_Tipo_Imagen);
                }

                await CompactarOrdenesImagenesProducto(audit, model.Id_Tipo_Imagen);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error registrando imagen de producto para plataforma {IdPlataforma}", model.Id_Plataforma);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "No fue posible registrar la imagen del producto.");
            }

            return ServiceResult.Success(
                esNuevo ? "Imagen de producto registrada correctamente." : "Imagen de producto reemplazada correctamente.",
                auditDescription: $"{(esNuevo ? "Registro" : "Reemplazo")} de imagen producto {imagen.Id_ImagenProducto}");
        }

        public async Task<ServiceResult> F_GetImagenesProductoList()
        {
            var imagenes = await QueryImagenes()
                .OrderBy(i => i.TipoImagen!.Descripcion)
                .ThenBy(i => i.Orden)
                .ThenBy(i => i.Plataforma!.Descripcion)
                .ThenByDescending(i => i.Fecha_Creacion)
                .ToListAsync();

            return ServiceResult.Success(data: imagenes.Select(MapImagen).ToList());
        }

        public async Task<ServiceResult> F_GetImagenProducto(int idImagenProducto)
        {
            var imagen = await QueryImagenes().FirstOrDefaultAsync(i => i.Id_ImagenProducto == idImagenProducto);
            return imagen == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Imagen no encontrada.")
                : ServiceResult.Success(data: MapImagen(imagen), auditDescription: $"Consulta de imagen producto {imagen.Id_ImagenProducto}");
        }

        public async Task<ServiceResult> P_UdpImagenProducto(int idImagenProducto, DtoImagenProductoUpdateRequest model, AuditContext audit)
        {
            var imagen = await _context.ImagenesProducto.FindAsync(idImagenProducto);
            if (imagen == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Imagen no existe.");
            }

            var validacion = await ValidarPlataforma(model.Id_Plataforma);
            if (validacion != null)
            {
                return validacion;
            }

            validacion = await ValidarTipoImagen(model.Id_Tipo_Imagen);
            if (validacion != null)
            {
                return validacion;
            }

            var estabaActiva = imagen.Vigente == 1;
            var tipoAnterior = imagen.Id_Tipo_Imagen ?? TipoPantallaIndividual;

            var existeOtraImagen = await _context.ImagenesProducto
                .AnyAsync(i => i.Id_Plataforma == model.Id_Plataforma
                    && i.Id_Tipo_Imagen == model.Id_Tipo_Imagen
                    && i.Id_ImagenProducto != idImagenProducto);

            if (existeOtraImagen)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe una imagen registrada para esta plataforma y tipo. Usa ese registro para reemplazarla.");
            }

            imagen.Id_Plataforma = model.Id_Plataforma;
            imagen.Id_Tipo_Imagen = model.Id_Tipo_Imagen;
            imagen.ImagenUrl = NormalizarUrlLocal(model.ImagenUrl);
            imagen.Descripcion = model.Descripcion?.Trim();
            imagen.Vigente = model.Vigente;
            imagen.Id_Usuario_Modifica = audit.UserId;
            imagen.Fecha_Modifica = DateTime.UtcNow;
            imagen.Maquina_Modifica = audit.Machine;
            if (tipoAnterior != model.Id_Tipo_Imagen || imagen.Orden <= 0 || (!estabaActiva && imagen.Vigente == 1))
            {
                imagen.Orden = await ObtenerSiguienteOrden(model.Id_Tipo_Imagen, idImagenProducto);
            }

            if (tipoAnterior != model.Id_Tipo_Imagen)
            {
                await CompactarOrdenesImagenesProducto(audit, tipoAnterior);
            }

            await CompactarOrdenesImagenesProducto(audit, model.Id_Tipo_Imagen);
            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Imagen de producto actualizada correctamente.",
                auditDescription: $"Actualizacion de imagen producto {imagen.Id_ImagenProducto}");
        }

        public async Task<ServiceResult> P_DeleteImagenProducto(int idImagenProducto, AuditContext audit)
        {
            var imagen = await _context.ImagenesProducto.FindAsync(idImagenProducto);
            if (imagen == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Imagen no existe.");
            }

            imagen.Vigente = 0;
            imagen.Id_Usuario_Modifica = audit.UserId;
            imagen.Fecha_Modifica = DateTime.UtcNow;
            imagen.Maquina_Modifica = audit.Machine;
            await CompactarOrdenesImagenesProducto(audit, imagen.Id_Tipo_Imagen ?? TipoPantallaIndividual, idImagenProducto);
            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Imagen marcada como inactiva correctamente.",
                auditDescription: $"Eliminacion logica de imagen producto {imagen.Id_ImagenProducto}");
        }

        public async Task<ServiceResult> P_MoverImagenProducto(DtoImagenProductoOrdenRequest model, AuditContext audit)
        {
            if (model.Direccion == 0)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Debe indicar si la imagen sube o baja en el orden.");
            }

            var imagen = await _context.ImagenesProducto.FindAsync(model.Id_ImagenProducto);
            if (imagen == null || imagen.Vigente != 1)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Imagen no existe o esta inactiva.");
            }

            var tipoImagen = imagen.Id_Tipo_Imagen ?? TipoPantallaIndividual;
            var imagenes = await _context.ImagenesProducto
                .Where(i => i.Vigente == 1)
                .Where(i => (i.Id_Tipo_Imagen ?? TipoPantallaIndividual) == tipoImagen)
                .OrderBy(i => i.Orden)
                .ThenBy(i => i.Fecha_Creacion)
                .ThenBy(i => i.Id_ImagenProducto)
                .ToListAsync();

            var indice = imagenes.FindIndex(i => i.Id_ImagenProducto == imagen.Id_ImagenProducto);
            var indiceDestino = indice + model.Direccion;
            if (indice < 0 || indiceDestino < 0 || indiceDestino >= imagenes.Count)
            {
                return ServiceResult.Success("La imagen ya esta en el limite del orden.");
            }

            (imagenes[indice], imagenes[indiceDestino]) = (imagenes[indiceDestino], imagenes[indice]);

            for (var i = 0; i < imagenes.Count; i++)
            {
                imagenes[i].Orden = i + 1;
                imagenes[i].Id_Usuario_Modifica = audit.UserId;
                imagenes[i].Fecha_Modifica = DateTime.UtcNow;
                imagenes[i].Maquina_Modifica = audit.Machine;
            }

            await _context.SaveChangesAsync();

            return ServiceResult.Success(
                "Orden actualizado correctamente.",
                auditDescription: $"Reordenamiento de imagen producto {imagen.Id_ImagenProducto}");
        }

        private IQueryable<Models.Administracion.ImagenesProducto> QueryImagenes()
        {
            return _context.ImagenesProducto
                .AsNoTracking()
                .Include(i => i.Plataforma)
                .Include(i => i.TipoImagen);
        }

        private static DtoImagenProductoItem MapImagen(Models.Administracion.ImagenesProducto imagen)
        {
            return new DtoImagenProductoItem
            {
                Id_ImagenProducto = imagen.Id_ImagenProducto,
                Id_Plataforma = imagen.Id_Plataforma,
                Plataforma = imagen.Plataforma?.Descripcion ?? string.Empty,
                Id_Tipo_Imagen = imagen.Id_Tipo_Imagen,
                TipoImagen = imagen.TipoImagen?.Descripcion ?? string.Empty,
                Orden = imagen.Orden,
                ImagenUrl = imagen.ImagenUrl,
                Descripcion = imagen.Descripcion,
                Vigente = imagen.Vigente,
                Fecha_Creacion = imagen.Fecha_Creacion
            };
        }

        private async Task<ServiceResult?> ValidarPlataforma(int idPlataforma)
        {
            var existe = await _context.Dominios
                .AnyAsync(d => d.Id_Dominio == idPlataforma
                    && d.Id_Padre == DominioPlataformas
                    && d.Vigente == 1);

            return existe
                ? null
                : ServiceResult.Fail(StatusCodes.Status400BadRequest, "La plataforma no existe o esta inactiva.");
        }

        private async Task<ServiceResult?> ValidarTipoImagen(int idTipoImagen)
        {
            var existe = await _context.Dominios
                .AnyAsync(d => d.Id_Dominio == idTipoImagen
                    && d.Id_Padre == DominioTipoImagen
                    && d.Vigente == 1);

            return existe
                ? null
                : ServiceResult.Fail(StatusCodes.Status400BadRequest, "El tipo de imagen no existe o esta inactivo.");
        }

        private static string NormalizarUrlLocal(string url)
        {
            return url.Trim().Replace("\\", "/");
        }

        private async Task<int> ObtenerSiguienteOrden(int idTipoImagen, int? idImagenActual = null)
        {
            var query = _context.ImagenesProducto
                .Where(i => i.Vigente == 1)
                .Where(i => (i.Id_Tipo_Imagen ?? TipoPantallaIndividual) == idTipoImagen);
            if (idImagenActual.HasValue)
            {
                query = query.Where(i => i.Id_ImagenProducto != idImagenActual.Value);
            }

            var ultimo = await query.Select(i => (int?)i.Orden).MaxAsync() ?? 0;
            return ultimo + 1;
        }

        private async Task CompactarOrdenesImagenesProducto(AuditContext audit, int idTipoImagen, int? idImagenExcluir = null)
        {
            var activas = await _context.ImagenesProducto
                .Where(i => i.Vigente == 1)
                .Where(i => (i.Id_Tipo_Imagen ?? TipoPantallaIndividual) == idTipoImagen)
                .Where(i => !idImagenExcluir.HasValue || i.Id_ImagenProducto != idImagenExcluir.Value)
                .OrderBy(i => i.Orden)
                .ThenBy(i => i.Fecha_Creacion)
                .ThenBy(i => i.Id_ImagenProducto)
                .ToListAsync();

            for (var i = 0; i < activas.Count; i++)
            {
                var nuevoOrden = i + 1;
                if (activas[i].Orden == nuevoOrden)
                {
                    continue;
                }

                activas[i].Orden = nuevoOrden;
                activas[i].Id_Usuario_Modifica = audit.UserId;
                activas[i].Fecha_Modifica = DateTime.UtcNow;
                activas[i].Maquina_Modifica = audit.Machine;
            }
        }
    }
}

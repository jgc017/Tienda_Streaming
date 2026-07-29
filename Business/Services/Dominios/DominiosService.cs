using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Dominios;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.Dominios;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Services.Dominios
{
    // Servicio de negocio del CRUD de dominios.
    // Centraliza reglas de jerarquia, duplicados, auditoria y baja logica.
    public class DominiosService : IDominios
    {
        private const string DominioRaiz = "SIN DATOS";
        private const string PostgresUniqueViolation = "23505";
        private readonly AppDbContext _context;
        private readonly ILogger<DominiosService> _logger;

        public DominiosService(AppDbContext context, ILogger<DominiosService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // P_InsDominio: valida el dominio padre y registra un nuevo dominio hijo.
        public async Task<ServiceResult> P_InsDominio(DtoDominioCreateRequest model, AuditContext audit)
        {
            var padre = await _context.Dominios
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id_Dominio == model.Id_Padre);

            if (padre == null)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El dominio padre no existe.");
            }

            if (!EsDominioPadre(padre))
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "No se pueden crear subdominios para este domino");
            }

            var descripcion = model.Descripcion.Trim();
            var descripcionNormalizada = descripcion.ToLowerInvariant();
            var dominioPadre = NormalizarDominioPadre(model.DominioPadre);

            var duplicado = await _context.Dominios
                .AnyAsync(d => d.Id_Padre == model.Id_Padre &&
                    d.Descripcion.Trim().ToLower() == descripcionNormalizada);

            if (duplicado)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un dominio hijo con esa descripcion.");
            }

            var nuevo = new Models.Administracion.Dominios
            {
                Id_Padre = model.Id_Padre,
                Descripcion = descripcion,
                DominioPadre = dominioPadre,
                Vigente = model.Vigente,
                Id_Usuario_Crea = audit.UserId,
                Fecha_Creacion = DateTime.UtcNow,
                Maquina_Creacion = audit.Machine
            };

            _context.Dominios.Add(nuevo);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EsDuplicadoDescripcion(ex))
            {
                _logger.LogWarning(ex, "Conflicto registrando dominio hijo {Descripcion}", descripcion);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un dominio hijo con esa descripcion.");
            }
            catch (DbUpdateException ex) when (EsViolacionClavePrimariaDominios(ex))
            {
                _logger.LogError(ex, "Secuencia de identificadores desincronizada registrando dominio hijo {Descripcion}", descripcion);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "No fue posible registrar el dominio porque la secuencia de identificadores esta desincronizada. Ejecuta las migraciones pendientes.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos registrando dominio hijo {Descripcion}", descripcion);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "No fue posible registrar el dominio. Verifica la estructura de la tabla Dominios.");
            }

            return ServiceResult.Success(
                "Dominio registrado correctamente.",
                extras: new Dictionary<string, object?> { ["id_Dominio"] = nuevo.Id_Dominio },
                auditDescription: $"Registro del dominio {nuevo.Descripcion} con id {nuevo.Id_Dominio}");
        }

        // F_GetDominiosList: consulta los dominios hijos del dominio seleccionado.
        public async Task<ServiceResult> F_GetDominiosList(int idDominio)
        {
            var dominios = await (from dominio in _context.Dominios.AsNoTracking()
                                  join padre in _context.Dominios.AsNoTracking()
                                      on dominio.Id_Padre equals padre.Id_Dominio
                                  where dominio.Id_Padre == idDominio
                                      && dominio.Descripcion.ToUpper() != DominioRaiz
                                  orderby dominio.Descripcion
                                  select new
                                  {
                                      dominio.Id_Dominio,
                                      dominio.Id_Padre,
                                      Dominio = padre.Descripcion,
                                      Dominio_Hijo = dominio.Descripcion,
                                      dominio.DominioPadre,
                                      dominio.Vigente
                                  })
                .ToListAsync();

            return ServiceResult.Success(data: dominios);
        }

        // F_GetDominiosList sin parametro: respuesta vacia usada cuando no hay padre seleccionado.
        public ServiceResult F_GetDominiosList()
        {
            return ServiceResult.Success(data: Array.Empty<object>());
        }

        // F_GetDominio: consulta un dominio por id para cargar el formulario de actualizacion.
        public async Task<ServiceResult> F_GetDominio(int idDominio)
        {
            var dominio = await _context.Dominios
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id_Dominio == idDominio);

            if (dominio == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Dominio no encontrado.");
            }

            return ServiceResult.Success(data: new
            {
                dominio.Id_Dominio,
                dominio.Id_Padre,
                dominio.Descripcion,
                dominio.DominioPadre,
                dominio.Vigente
            }, auditDescription: $"Consulta del dominio {dominio.Descripcion} con id {dominio.Id_Dominio}");
        }

        // P_UdpDominio: actualiza padre, descripcion, si puede ser padre y estado vigente.
        public async Task<ServiceResult> P_UdpDominio(int idDominio, DtoDominioUpdateRequest model, AuditContext audit)
        {
            if (idDominio == model.Id_Padre)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Un dominio no puede ser hijo de si mismo.");
            }

            var dominio = await _context.Dominios.FindAsync(idDominio);
            if (dominio == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Dominio no existe.");
            }

            var padre = await _context.Dominios
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id_Dominio == model.Id_Padre);

            if (padre == null)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "El dominio padre no existe.");
            }

            if (!EsDominioPadre(padre))
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "No se pueden crear subdominios para este domino");
            }

            var descripcion = model.Descripcion.Trim();
            var descripcionNormalizada = descripcion.ToLowerInvariant();
            var dominioPadre = NormalizarDominioPadre(model.DominioPadre);

            var duplicado = await _context.Dominios
                .AnyAsync(d => d.Id_Dominio != idDominio &&
                    d.Id_Padre == model.Id_Padre &&
                    d.Descripcion.Trim().ToLower() == descripcionNormalizada);

            if (duplicado)
            {
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un dominio hijo con esa descripcion.");
            }

            dominio.Id_Padre = model.Id_Padre;
            dominio.Descripcion = descripcion;
            dominio.DominioPadre = dominioPadre;
            dominio.Vigente = model.Vigente;
            dominio.Id_Usuario_Modifica = audit.UserId;
            dominio.Fecha_Modifica = DateTime.UtcNow;
            dominio.Maquina_Modifica = audit.Machine;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (EsDuplicadoDescripcion(ex))
            {
                _logger.LogWarning(ex, "Conflicto actualizando dominio {IdDominio}", idDominio);
                return ServiceResult.Fail(StatusCodes.Status409Conflict, "Ya existe un dominio hijo con esa descripcion.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos actualizando dominio {IdDominio}", idDominio);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "No fue posible actualizar el dominio. Verifica la estructura de la tabla Dominios.");
            }

            return ServiceResult.Success(
                "Dominio actualizado correctamente.",
                auditDescription: $"Actualizacion del dominio {dominio.Descripcion} con id {dominio.Id_Dominio}");
        }

        // P_DeleteDominio: realiza baja logica marcando Vigente = 0.
        public async Task<ServiceResult> P_DeleteDominio(int idDominio, AuditContext audit)
        {
            try
            {
                var dominio = await _context.Dominios.FirstOrDefaultAsync(d => d.Id_Dominio == idDominio);
                if (dominio == null)
                {
                    return ServiceResult.Fail(StatusCodes.Status404NotFound, "El dominio no existe.");
                }

                if (dominio.Vigente == 0)
                {
                    return ServiceResult.Success(
                        "El dominio ya se encontraba inactivo.",
                        auditDescription: $"Eliminacion logica del dominio {dominio.Descripcion} con id {dominio.Id_Dominio}");
                }

                dominio.Vigente = 0;
                dominio.Id_Usuario_Modifica = audit.UserId;
                dominio.Fecha_Modifica = DateTime.UtcNow;
                dominio.Maquina_Modifica = audit.Machine;

                await _context.SaveChangesAsync();

                return ServiceResult.Success(
                    "El dominio fue marcado como inactivo correctamente.",
                    auditDescription: $"Eliminacion logica del dominio {dominio.Descripcion} con id {dominio.Id_Dominio}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando dominio {IdDominio}", idDominio);
                return ServiceResult.Fail(StatusCodes.Status500InternalServerError, "Ocurrio un error al eliminar el dominio.");
            }
        }

        private static string NormalizarDominioPadre(string value)
        {
            return string.Equals(value, "Si", StringComparison.OrdinalIgnoreCase) ? "Si" : "No";
        }

        private static bool EsDominioPadre(Models.Administracion.Dominios dominio)
        {
            return string.Equals(dominio.DominioPadre, "Si", StringComparison.OrdinalIgnoreCase);
        }

        private static bool EsDuplicadoDescripcion(DbUpdateException ex)
        {
            return ObtenerPostgresException(ex) is { SqlState: PostgresUniqueViolation } postgresException
                && postgresException.TableName == "Dominios"
                && postgresException.ConstraintName?.Contains("Descripcion", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool EsViolacionClavePrimariaDominios(DbUpdateException ex)
        {
            return ObtenerPostgresException(ex) is { SqlState: PostgresUniqueViolation } postgresException
                && postgresException.TableName == "Dominios"
                && string.Equals(postgresException.ConstraintName, "PK_Dominios", StringComparison.Ordinal);
        }

        private static PostgresException? ObtenerPostgresException(DbUpdateException ex)
        {
            return ex.GetBaseException() as PostgresException;
        }
    }
}

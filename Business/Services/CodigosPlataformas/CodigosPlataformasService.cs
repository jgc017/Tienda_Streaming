using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.CodigosPlataformas;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Administracion.CodigosPlataformas;
using Tienda_Streaming.Services.Email;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Tienda_Streaming.Business.Services.CodigosPlataformas
{
    public class CodigosPlataformasService : ICodigosPlataformas
    {
        private readonly AppDbContext _context;
        private readonly CodigosPlataformasMailSettings _settings;
        private readonly ILogger<CodigosPlataformasService> _logger;

        public CodigosPlataformasService(
            AppDbContext context,
            IOptions<CodigosPlataformasMailSettings> settings,
            ILogger<CodigosPlataformasService> logger)
        {
            _context = context;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<ServiceResult> F_GetCorreosAdminList()
        {
            var data = await _context.CorreosPlataforma
                .AsNoTracking()
                .OrderByDescending(c => c.Fecha_Recepcion)
                .Select(c => MapItem(c))
                .ToListAsync();

            return ServiceResult.Success(data: data);
        }

        public async Task<ServiceResult> F_GetCorreoAdminDetalle(int idCorreo)
        {
            var correo = await _context.CorreosPlataforma
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id_Correo_Plataforma == idCorreo);

            return correo == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Correo no encontrado.")
                : ServiceResult.Success(data: MapDetalle(correo));
        }

        public async Task<ServiceResult> P_DeleteCorreo(int idCorreo, AuditContext audit)
        {
            var correo = await _context.CorreosPlataforma.FindAsync(idCorreo);
            if (correo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status404NotFound, "Correo no encontrado.");
            }

            _context.CorreosPlataforma.Remove(correo);
            await _context.SaveChangesAsync();

            return ServiceResult.Success("Correo eliminado correctamente.", auditDescription: $"Eliminacion fisica correo plataforma {idCorreo}");
        }

        public async Task<ServiceResult> F_BuscarCorreosPublico(DtoBuscarCorreoPlataformaRequest model)
        {
            var correo = NormalizarCorreo(model.Correo);
            if (correo == null)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Ingresa un correo valido.");
            }

            var data = await _context.CorreosPlataforma
                .AsNoTracking()
                .Where(c => c.Texto_Busqueda.Contains(correo))
                .OrderByDescending(c => c.Fecha_Recepcion)
                .Select(c => MapItem(c))
                .ToListAsync();

            return ServiceResult.Success(data: data);
        }

        public async Task<ServiceResult> F_GetCorreoPublicoDetalle(DtoDetalleCorreoPlataformaRequest model)
        {
            var correoBuscado = NormalizarCorreo(model.Correo);
            if (correoBuscado == null)
            {
                return ServiceResult.Fail(StatusCodes.Status400BadRequest, "Ingresa un correo valido.");
            }

            var correo = await _context.CorreosPlataforma
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id_Correo_Plataforma == model.Id_Correo_Plataforma &&
                    c.Texto_Busqueda.Contains(correoBuscado));

            return correo == null
                ? ServiceResult.Fail(StatusCodes.Status404NotFound, "Correo no encontrado para la busqueda realizada.")
                : ServiceResult.Success(data: MapDetalle(correo));
        }

        public async Task<int> SincronizarBuzon(CancellationToken cancellationToken)
        {
            if (!ConfiguracionImapValida())
            {
                return 0;
            }

            using var client = new ImapClient();
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
                cancellationToken);

            await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);

            var folder = string.Equals(_settings.Folder, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(_settings.Folder, cancellationToken);

            if (folder == null)
            {
                _logger.LogWarning("No se encontro la carpeta IMAP configurada para codigos de plataformas: {Folder}", _settings.Folder);
                return 0;
            }

            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);
            var uids = await folder.SearchAsync(SearchQuery.All, cancellationToken);
            var cantidadImportada = 0;

            foreach (var uid in uids.OrderByDescending(u => u.Id).Take(Math.Max(_settings.MaxMessagesPerSync, 1)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var mensaje = await folder.GetMessageAsync(uid, cancellationToken);
                var entity = ConstruirEntidad(mensaje);
                var existe = await _context.CorreosPlataforma
                    .AnyAsync(c => c.Hash_Mensaje == entity.Hash_Mensaje, cancellationToken);

                if (!existe)
                {
                    _context.CorreosPlataforma.Add(entity);
                    await _context.SaveChangesAsync(cancellationToken);
                    cantidadImportada++;
                }

                if (_settings.DeleteFromMailboxAfterImport)
                {
                    await folder.AddFlagsAsync(uid, MessageFlags.Deleted, true, cancellationToken);
                }
            }

            if (_settings.DeleteFromMailboxAfterImport)
            {
                await folder.ExpungeAsync(cancellationToken);
            }

            await client.DisconnectAsync(true, cancellationToken);
            return cantidadImportada;
        }

        public async Task<int> EliminarCorreosAntiguos(CancellationToken cancellationToken)
        {
            var retentionHours = Math.Max(_settings.RetentionHours, 1);
            var fechaLimite = DateTime.UtcNow.AddHours(-retentionHours);
            var correos = await _context.CorreosPlataforma
                .Where(c => c.Fecha_Registro < fechaLimite)
                .ToListAsync(cancellationToken);

            if (!correos.Any())
            {
                return 0;
            }

            _context.CorreosPlataforma.RemoveRange(correos);
            await _context.SaveChangesAsync(cancellationToken);
            return correos.Count;
        }

        private bool ConfiguracionImapValida()
        {
            return _settings.Enabled &&
                !string.IsNullOrWhiteSpace(_settings.Host) &&
                !string.IsNullOrWhiteSpace(_settings.UserName) &&
                !string.IsNullOrWhiteSpace(_settings.Password);
        }

        private static CorreosPlataforma ConstruirEntidad(MimeMessage mensaje)
        {
            var remitente = string.Join("; ", mensaje.From.Mailboxes.Select(m => m.Address));
            var destinatarios = ExtraerDestinatarios(mensaje);
            var encabezados = string.Join(Environment.NewLine, mensaje.Headers.Select(h => $"{h.Field}: {h.Value}"));
            var cuerpoTexto = mensaje.TextBody ?? ExtraerTextoDesdeHtml(mensaje.HtmlBody);
            var cuerpoHtml = SanitizarHtml(mensaje.HtmlBody);
            var asunto = mensaje.Subject ?? string.Empty;
            var fechaRecepcion = mensaje.Date != default ? mensaje.Date.UtcDateTime : DateTime.UtcNow;
            var textoBusqueda = NormalizarBusqueda($"{remitente} {destinatarios} {asunto} {encabezados} {cuerpoTexto} {mensaje.HtmlBody}");
            var hash = CrearHash($"{mensaje.MessageId}|{fechaRecepcion:o}|{remitente}|{destinatarios}|{asunto}");

            return new CorreosPlataforma
            {
                MessageId = Limitar(mensaje.MessageId, 160),
                Hash_Mensaje = hash,
                Remitente = Limitar(remitente, 300) ?? string.Empty,
                Destinatarios = Limitar(destinatarios, 1000) ?? string.Empty,
                Asunto = Limitar(asunto, 300) ?? string.Empty,
                Encabezados = encabezados,
                Cuerpo_Texto = cuerpoTexto,
                Cuerpo_Html = cuerpoHtml,
                Texto_Busqueda = textoBusqueda,
                Fecha_Recepcion = fechaRecepcion,
                Fecha_Registro = DateTime.UtcNow
            };
        }

        private static string ExtraerDestinatarios(MimeMessage mensaje)
        {
            var destinatarios = new List<string>();
            destinatarios.AddRange(mensaje.To.Mailboxes.Select(m => m.Address));
            destinatarios.AddRange(mensaje.Cc.Mailboxes.Select(m => m.Address));
            destinatarios.AddRange(mensaje.Bcc.Mailboxes.Select(m => m.Address));

            foreach (var headerName in new[] { "Delivered-To", "X-Original-To", "Envelope-To", "Resent-To", "X-Forwarded-To" })
            {
                destinatarios.AddRange(mensaje.Headers
                    .Where(h => string.Equals(h.Field, headerName, StringComparison.OrdinalIgnoreCase))
                    .Select(h => h.Value));
            }

            return string.Join("; ", destinatarios.Where(d => !string.IsNullOrWhiteSpace(d)).Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static DtoCorreoPlataformaItem MapItem(CorreosPlataforma correo)
        {
            return new DtoCorreoPlataformaItem
            {
                Id_Correo_Plataforma = correo.Id_Correo_Plataforma,
                Remitente = correo.Remitente,
                Destinatarios = correo.Destinatarios,
                Asunto = correo.Asunto,
                Fecha_Recepcion = correo.Fecha_Recepcion,
                Fecha_Registro = correo.Fecha_Registro
            };
        }

        private static DtoCorreoPlataformaDetalle MapDetalle(CorreosPlataforma correo)
        {
            var html = !string.IsNullOrWhiteSpace(correo.Cuerpo_Html)
                ? correo.Cuerpo_Html!
                : TextoPlanoAHtml(correo.Cuerpo_Texto);

            return new DtoCorreoPlataformaDetalle
            {
                Id_Correo_Plataforma = correo.Id_Correo_Plataforma,
                Remitente = correo.Remitente,
                Destinatarios = correo.Destinatarios,
                Asunto = correo.Asunto,
                Fecha_Recepcion = correo.Fecha_Recepcion,
                Fecha_Registro = correo.Fecha_Registro,
                Cuerpo_Texto = correo.Cuerpo_Texto ?? string.Empty,
                Cuerpo_Html = html,
                Enlaces = ExtraerEnlaces(html)
            };
        }

        private static List<DtoCorreoPlataformaEnlace> ExtraerEnlaces(string html)
        {
            return Regex.Matches(html, "<a\\b[^>]*href\\s*=\\s*[\"'](?<url>[^\"']+)[\"'][^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Select(m => new DtoCorreoPlataformaEnlace
                {
                    Url = WebUtility.HtmlDecode(m.Groups["url"].Value),
                    Texto = LimpiarTexto(WebUtility.HtmlDecode(Regex.Replace(m.Groups["text"].Value, "<.*?>", string.Empty)))
                })
                .Where(e => Uri.TryCreate(e.Url, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .GroupBy(e => e.Url)
                .Select(g => g.First())
                .Take(20)
                .ToList();
        }

        private static string SanitizarHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var limpio = html;
            limpio = Regex.Replace(limpio, "<\\s*(script|style|iframe|object|embed|link|meta|base|form)\\b[^>]*>.*?<\\s*/\\s*\\1\\s*>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            limpio = Regex.Replace(limpio, "<\\s*(script|style|iframe|object|embed|link|meta|base|form)\\b[^>]*?/?>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            limpio = Regex.Replace(limpio, "\\s+on\\w+\\s*=\\s*([\"']).*?\\1", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            limpio = Regex.Replace(limpio, "\\s+(href|src)\\s*=\\s*([\"'])\\s*javascript:.*?\\2", " $1=\"#\"", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return limpio;
        }

        private static string ExtraerTextoDesdeHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var sinEtiquetas = Regex.Replace(html, "<br\\s*/?>", Environment.NewLine, RegexOptions.IgnoreCase);
            sinEtiquetas = Regex.Replace(sinEtiquetas, "<.*?>", " ");
            return LimpiarTexto(WebUtility.HtmlDecode(sinEtiquetas));
        }

        private static string TextoPlanoAHtml(string? texto)
        {
            return $"<pre>{WebUtility.HtmlEncode(texto ?? string.Empty)}</pre>";
        }

        private static string NormalizarBusqueda(string texto)
        {
            return LimpiarTexto(WebUtility.HtmlDecode(texto ?? string.Empty)).ToLowerInvariant();
        }

        private static string? NormalizarCorreo(string? correo)
        {
            var value = correo?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value) || value.Length > 160)
            {
                return null;
            }

            try
            {
                var address = new MailAddress(value);
                return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase)
                    ? value
                    : null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static string LimpiarTexto(string? texto)
        {
            return Regex.Replace(texto ?? string.Empty, "\\s+", " ").Trim();
        }

        private static string? Limitar(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value[..maxLength];
        }

        private static string CrearHash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }
    }
}

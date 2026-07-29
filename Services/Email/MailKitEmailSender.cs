using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Tienda_Streaming.Services.Email
{
    // Implementacion SMTP basada en MailKit.
    // AccountController la usa para enviar enlaces de recuperacion de contrasena.
    public class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<MailKitEmailSender> _logger;

        // Lee SmtpSettings desde configuracion y habilita logs de envio/error.
        public MailKitEmailSender(IOptions<SmtpSettings> settings, ILogger<MailKitEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        // Construye y envia el correo de recuperacion.
        // Si SMTP no esta configurado, no rompe el flujo: deja el enlace en logs
        // para pruebas locales.
        public async Task SendPasswordResetAsync(string toEmail, string resetUrl)
        {
            var builder = new BodyBuilder
            {
                HtmlBody = $"""
                    <p>Recibimos una solicitud para restablecer tu contrasena.</p>
                    <p><a href="{resetUrl}">Restablecer contrasena</a></p>
                    <p>Este enlace vence en 30 minutos. Si no solicitaste este cambio, ignora este correo.</p>
                    """,
                TextBody = $"Usa este enlace para restablecer tu contrasena: {resetUrl}"
            };

            var enviado = await SendAsync(toEmail, "Recuperacion de contrasena", builder, () =>
                _logger.LogWarning("SMTP no esta configurado. Enlace de recuperacion para {Email}: {ResetUrl}", toEmail, resetUrl));

            if (enviado)
            {
                _logger.LogInformation("Correo de recuperacion enviado por SMTP a {Email}", toEmail);
            }
        }

        // Construye y envia el correo con datos de acceso y enlace de cambio de contrasena.
        public async Task SendNewUserAccessAsync(string toEmail, string userName, string temporaryPassword, string accessUrl, string platformName, string userType)
        {
            var formatoDatos = $"""
                Le ha sido creado el siguiente usuario en la plataforma
                **Plataforma:** {platformName}
                **Tipo Usuario:** {userType}
                **usuario:** {userName}
                **Contraseña:** {temporaryPassword}
                Para acceder puede hacerlo a traves de la siguiente URL, una vez ingrese debera actualizar su contraseña:
                **Link Acceso:** {accessUrl}
                """;

            var builder = new BodyBuilder
            {
                HtmlBody = $"""
                    <p>Le ha sido creado el siguiente usuario en la plataforma</p>
                    <p><strong>Plataforma:</strong> {Html(platformName)}</p>
                    <p><strong>Tipo Usuario:</strong> {Html(userType)}</p>
                    <p><strong>usuario:</strong> {Html(userName)}</p>
                    <p><strong>Contraseña:</strong> {Html(temporaryPassword)}</p>
                    <p>Para acceder puede hacerlo a traves de la siguiente URL, una vez ingrese debera actualizar su contraseña:</p>
                    <p><strong>Link Acceso:</strong> <a href="{Html(accessUrl)}">{Html(accessUrl)}</a></p>
                    """,
                TextBody = formatoDatos
            };

            var enviado = await SendAsync(toEmail, "Acceso a Tienda Streaming", builder, () =>
                _logger.LogWarning("SMTP no esta configurado. Acceso temporal para {Email}. Usuario: {UserName}. LinkAcceso: {AccessUrl}", toEmail, userName, accessUrl));

            if (enviado)
            {
                _logger.LogInformation("Correo de acceso enviado por SMTP a {Email}", toEmail);
            }
        }

        public async Task SendPurchaseConfirmationAsync(string toEmail, string subject, string textBody, string htmlBody)
        {
            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            };

            var enviado = await SendAsync(toEmail, subject, builder, () =>
                _logger.LogWarning("SMTP no esta configurado. Resumen de compra para {Email}: {Body}", toEmail, textBody));

            if (enviado)
            {
                _logger.LogInformation("Correo de compra enviado por SMTP a {Email}", toEmail);
            }
        }

        // Envia un mensaje SMTP o ejecuta el fallback de log cuando no hay configuracion.
        private async Task<bool> SendAsync(string toEmail, string subject, BodyBuilder builder, Action logIfNotConfigured)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) ||
                string.IsNullOrWhiteSpace(_settings.UserName) ||
                string.IsNullOrWhiteSpace(_settings.Password) ||
                string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                logIfNotConfigured();
                return false;
            }

            // MimeMessage define remitente, destinatario, asunto y cuerpos HTML/texto.
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = builder.ToMessageBody();

            // Timeout corto para que una red bloqueada no deje esperando al usuario.
            using var client = new SmtpClient
            {
                Timeout = 15000
            };

            // Puerto 465 usa SSL desde el inicio; 587 usa STARTTLS.
            var socketOptions = _settings.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

            // Conecta, autentica con el proveedor SMTP y envia el mensaje.
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions);
            await client.AuthenticateAsync(_settings.UserName, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }

        private static string Html(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}

using System.Threading.Tasks;

namespace Tienda_Streaming.Services.Email
{
    // Contrato de envio de correos. Permite cambiar MailKit por otro proveedor
    // sin modificar AccountController.
    public interface IEmailSender
    {
        // Envia el enlace absoluto de recuperacion al correo del usuario.
        Task SendPasswordResetAsync(string toEmail, string resetUrl);

        // Envia datos de acceso y enlace de cambio de contrasena a usuarios nuevos.
        Task SendNewUserAccessAsync(string toEmail, string userName, string temporaryPassword, string accessUrl, string platformName, string userType);

        // Envia el resumen de compra con las cuentas asignadas.
        Task SendPurchaseConfirmationAsync(string toEmail, string subject, string textBody, string htmlBody);
    }
}

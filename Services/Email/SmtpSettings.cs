namespace Tienda_Streaming.Services.Email
{
    // Modelo de configuracion enlazado a la seccion "Smtp" de appsettings/User Secrets.
    // Program.cs lo registra con Configure<SmtpSettings>() y MailKitEmailSender lo consume.
    public class SmtpSettings
    {
        // Servidor SMTP, por ejemplo smtp.gmail.com.
        public string Host { get; set; } = string.Empty;

        // Puerto SMTP. Gmail normalmente usa 587 con STARTTLS o 465 con SSL directo.
        public int Port { get; set; } = 587;

        // Indica si se debe negociar conexion segura.
        public bool UseSsl { get; set; } = true;

        // Usuario SMTP. En Gmail suele ser el correo completo.
        public string UserName { get; set; } = string.Empty;

        // Contrasena SMTP o contrasena de aplicacion. Debe vivir en User Secrets.
        public string Password { get; set; } = string.Empty;

        // Correo remitente visible.
        public string FromEmail { get; set; } = string.Empty;

        // Nombre remitente visible en el cliente de correo.
        public string FromName { get; set; } = "Tienda Streaming";
    }
}

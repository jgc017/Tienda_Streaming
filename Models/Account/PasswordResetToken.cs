using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Account
{
    // Entidad persistida en la tabla PasswordResetTokens.
    // AccountController la crea en ForgotPassword y la consume en ResetPassword.
    public class PasswordResetToken
    {
        [Key]
        public int Id_PasswordResetToken { get; set; }

        // Usuario propietario del token de recuperacion.
        public int Id_Usuario { get; set; }

        // Hash SHA-256 del token real. El token real nunca se guarda.
        [Required]
        [StringLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        // Fechas de auditoria y vencimiento del enlace.
        public DateTime Fecha_Creacion { get; set; }

        public DateTime Fecha_Expiracion { get; set; }

        // Se llena cuando el enlace fue usado o invalidado por uno nuevo.
        public DateTime? Fecha_Uso { get; set; }

        // IP desde la que se solicito la recuperacion.
        [StringLength(80)]
        public string? Ip_Solicitud { get; set; }
    }
}

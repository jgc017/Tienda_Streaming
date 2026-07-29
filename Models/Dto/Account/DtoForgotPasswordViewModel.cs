using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Account
{
    // Modelo usado por Views/Account/ForgotPassword.cshtml para solicitar
    // el envio de un enlace de recuperacion.
    public class DtoForgotPasswordViewModel
    {
        // Correo que se buscara en Usuarios.E_Mail. El controlador no revela
        // si existe o no para evitar enumeracion de cuentas.
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un email valido")]
        public string Email { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Account
{
    // Modelo usado por Views/Account/ResetPassword.cshtml y
    // AccountController.ResetPassword(POST).
    public class DtoResetPasswordViewModel
    {
        // Token recibido por URL. En base de datos se compara contra TokenHash.
        [Required]
        public string Token { get; set; } = string.Empty;

        // Nueva contrasena con reglas minimas: longitud, mayuscula, minuscula y numero.
        [Required(ErrorMessage = "La nueva contrasena es obligatoria")]
        [StringLength(128, MinimumLength = 10)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contrasena debe incluir mayuscula, minuscula y numero")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Confirmacion visual para evitar errores de digitacion.
        [Required(ErrorMessage = "Confirma la nueva contrasena")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Las contrasenas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

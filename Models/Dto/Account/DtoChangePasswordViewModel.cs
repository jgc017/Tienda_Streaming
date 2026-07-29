using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Account
{
    // Modelo usado por AccountController.ChangePassword cuando el usuario
    // ingresa con una contrasena temporal emitida por el administrador.
    public class DtoChangePasswordViewModel
    {
        [Required(ErrorMessage = "La contrasena actual es obligatoria")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contrasena es obligatoria")]
        [StringLength(128, MinimumLength = 10)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contrasena debe incluir mayuscula, minuscula y numero")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la nueva contrasena")]
        [Compare(nameof(Password), ErrorMessage = "Las contrasenas no coinciden")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

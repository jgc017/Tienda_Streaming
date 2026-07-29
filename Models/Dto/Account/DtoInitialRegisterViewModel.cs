using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Account
{
    // Modelo usado por AccountController.RegistroInicial para crear el primer
    // usuario cuando la base de datos aun no tiene usuarios registrados.
    public class DtoInitialRegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(120, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El usuario es obligatorio")]
        [StringLength(60, MinimumLength = 3)]
        public string Usuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        [StringLength(160)]
        public string E_Mail { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contrasena es obligatoria")]
        [StringLength(128, MinimumLength = 10)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "La contrasena debe incluir mayuscula, minuscula y numero")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la contrasena")]
        [Compare(nameof(Password), ErrorMessage = "Las contrasenas no coinciden")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

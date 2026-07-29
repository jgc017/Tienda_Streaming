using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Account
{
    // Modelo que recibe Views/Account/Login.cshtml y procesa
    // AccountController.Login(POST).
    public class DtoLoginViewModel
    {
        // Puede contener nombre de usuario o email.
        [Required(ErrorMessage = "El usuario o email es obligatorio")]
        public string Usuario { get; set; } = string.Empty;

        // Contrasena en texto plano solo vive durante el request; se compara
        // contra el hash BCrypt guardado en Usuarios.Password.
        [Required(ErrorMessage = "La contrasena es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Ruta local a donde se vuelve despues de iniciar sesion.
        public string? ReturnUrl { get; set; }
    }
}

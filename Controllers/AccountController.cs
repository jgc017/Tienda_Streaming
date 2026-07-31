using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Tienda_Streaming.Data;
using Tienda_Streaming.Business.Common;
using Tienda_Streaming.Business.Interfaces.Usuarios;
using Tienda_Streaming.Models.Account;
using Tienda_Streaming.Models.Administracion;
using Tienda_Streaming.Models.Dto.Account;
using Tienda_Streaming.Models.Dto.Administracion.Usuarios;
using Tienda_Streaming.Security;
using Tienda_Streaming.Services.Email;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Tienda_Streaming.Controllers
{
    // Controlador responsable del flujo de autenticacion:
    // login, logout, olvido de contrasena, restablecimiento y acceso denegado.
    public class AccountController : Controller
    {
        // Claim propio para guardar los Id_Rol asignados al usuario autenticado.
        // Permite validar permisos por identificador estable, no por descripcion del rol.
        public const string RoleIdClaimType = "Id_Rol";
        public const string MustChangePasswordClaimType = "Debe_Cambiar_Password";
        public const string DevelopmentSessionClaimType = "Development_Auth_Session";

        private const string MensajeSinUsuarios = "No hay usuarios registrados, registre el primer usuario.";
        private const string MensajeUsuarioNoRegistrado = "Este usuario no se encuentra registrado.";
        private const string MensajePasswordIncorrecta = "Contrase\u00f1a incorrecta.";
        private const string MensajeSesionExpirada = "La sesion del formulario expiro o la pagina estaba desactualizada. Intenta nuevamente.";

        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IUsuarios _usuarios;
        private readonly ILogger<AccountController> _logger;
        private readonly DevelopmentAuthSession _developmentAuthSession;

        // Recibe el DbContext para consultar usuarios/tokens, el servicio de
        // correo para enviar recuperaciones y el logger para diagnostico.
        public AccountController(
            AppDbContext context,
            IEmailSender emailSender,
            IUsuarios usuarios,
            ILogger<AccountController> logger,
            DevelopmentAuthSession developmentAuthSession)
        {
            _context = context;
            _emailSender = emailSender;
            _usuarios = usuarios;
            _logger = logger;
            _developmentAuthSession = developmentAuthSession;
        }

        // GET: /Account/Login
        // Muestra el formulario de imagenes y videos de sesion. Si ya hay sesion activa,
        // redirige al destino solicitado o al Home.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null, string? mensaje = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            var existenUsuarios = await _usuarios.ExistenUsuarios();
            var permiteRegistroInicial = !existenUsuarios;
            ViewBag.PermiteRegistroInicial = permiteRegistroInicial;
            ViewBag.LoginMensaje = permiteRegistroInicial
                ? MensajeSinUsuarios
                : mensaje == "sesion-expirada"
                    ? MensajeSesionExpirada
                    : null;
            return View("VwLogin", new DtoLoginViewModel { ReturnUrl = returnUrl });
        }

        // GET: /Account/RegistroInicial
        // Permite crear el primer usuario unicamente cuando la tabla Usuarios esta vacia.
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> RegistroInicial(string? mensaje = null)
        {
            if (await _usuarios.ExistenUsuarios())
            {
                return RedirectToAction(nameof(Login));
            }

            ViewBag.LoginMensaje = mensaje == "sesion-expirada" ? MensajeSesionExpirada : null;
            return View("VwInitialRegister", new DtoInitialRegisterViewModel());
        }

        // POST: /Account/RegistroInicial
        // Crea el primer usuario del sistema y, si existe el rol 1, lo deja como super usuario.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RegistroInicial(DtoInitialRegisterViewModel model)
        {
            if (await _usuarios.ExistenUsuarios())
            {
                return RedirectToAction(nameof(Login));
            }

            if (!ModelState.IsValid)
            {
                return View("VwInitialRegister", model);
            }

            var request = new DtoUsuarioCreateRequest
            {
                Nombre = model.Nombre,
                Usuario = model.Usuario,
                E_Mail = model.E_Mail,
                Password = model.Password
            };

            var resetPasswordUrlBase = Url.Action(nameof(ResetPassword), "Account", null, Request.Scheme);
            var result = await _usuarios.P_InsUsuario(request, GetAuditContext(), esRegistroInicial: true, resetPasswordUrlBase);

            if (!result.Ok)
            {
                ModelState.AddModelError(string.Empty, result.Mensaje ?? "No fue posible crear el primer usuario.");
                return View("VwInitialRegister", model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Usuario.ToLower() == model.Usuario.Trim().ToLowerInvariant());

            if (usuario != null)
            {
                await SignInUserAsync(usuario);
            }

            return RedirectToAction("VwTiendas", "Tiendas");
        }

        // POST: /Account/Login
        // Valida usuario/email y contrasena, crea los claims y emite la cookie
        // de autenticacion usada por el resto del proyecto.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login(DtoLoginViewModel model)
        {
            var existenUsuarios = await _usuarios.ExistenUsuarios();
            ViewBag.PermiteRegistroInicial = !existenUsuarios;

            if (!ModelState.IsValid)
            {
                if (!existenUsuarios)
                {
                    ModelState.AddModelError(string.Empty, MensajeSinUsuarios);
                }

                return View("VwLogin", model);
            }

            if (!existenUsuarios)
            {
                ModelState.AddModelError(string.Empty, MensajeSinUsuarios);
                return View("VwLogin", model);
            }

            // Permite iniciar sesion usando nombre de usuario o correo.
            var login = model.Usuario.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Vigente == 1 &&
                    (u.Usuario.ToLower() == login || (u.E_Mail != null && u.E_Mail.ToLower() == login)));

            if (usuario == null)
            {
                _logger.LogWarning("Intento de login con usuario no registrado para {Usuario}", model.Usuario);
                ModelState.AddModelError(string.Empty, MensajeUsuarioNoRegistrado);
                return View("VwLogin", model);
            }

            if (!BCrypt.Net.BCrypt.Verify(model.Password, usuario.Password))
            {
                _logger.LogWarning("Intento de login con contrasena incorrecta para {Usuario}", model.Usuario);
                ModelState.AddModelError(string.Empty, MensajePasswordIncorrecta);
                return View("VwLogin", model);
            }

            await SignInUserAsync(usuario);

            if (usuario.Debe_Cambiar_Password == 1)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            return RedirectToLocal(model.ReturnUrl);
        }

        // GET: /Account/ChangePassword
        // Pantalla obligatoria para usuarios que ingresan con contrasena temporal.
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View("VwChangePassword", new DtoChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        // Cambia la contrasena temporal y libera al usuario para navegar el sistema.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ChangePassword(DtoChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("VwChangePassword", model);
            }

            var usuarioId = GetCurrentUserId();
            if (!usuarioId.HasValue)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction(nameof(Login));
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == usuarioId.Value && u.Vigente == 1);

            if (usuario == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, usuario.Password))
            {
                ModelState.AddModelError(string.Empty, "La contrasena actual no es correcta.");
                return View("VwChangePassword", model);
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12);
            usuario.Debe_Cambiar_Password = 0;
            usuario.Id_Usuario_Modifica = usuario.Id_Usuario;
            usuario.Fecha_Modifica = DateTime.UtcNow;
            usuario.Maquina_Modifica = GetClientIp();

            await _context.SaveChangesAsync();
            await SignInUserAsync(usuario);

            return RedirectToAction("VwTiendas", "Tiendas");
        }

        // Crea la cookie de autenticacion con los datos actuales del usuario y sus roles vigentes.
        private async Task SignInUserAsync(Usuarios usuario)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id_Usuario.ToString()),
                new(ClaimTypes.Name, usuario.Usuario),
                new(ClaimTypes.Email, usuario.E_Mail ?? string.Empty)
            };

            if (usuario.Debe_Cambiar_Password == 1)
            {
                claims.Add(new Claim(MustChangePasswordClaimType, "1"));
            }

            // Agrega roles vigentes del usuario a la cookie:
            // - ClaimTypes.Role conserva el nombre para compatibilidad con [Authorize(Roles=...)].
            // - RoleIdClaimType conserva el Id_Rol para validaciones internas mas estables.
            var roles = await (from ru in _context.Roles_User.AsNoTracking()
                               join rol in _context.Roles.AsNoTracking() on ru.Id_Rol equals rol.Id_Rol
                               where ru.Id_Usuario == usuario.Id_Usuario && ru.Vigente == 1 && rol.Vigente == 1
                               select new
                               {
                                   rol.Id_Rol,
                                   rol.Rol
                               })
                .ToListAsync();

            claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol.Rol)));
            claims.AddRange(roles.Select(rol => new Claim(RoleIdClaimType, rol.Id_Rol.ToString())));
            claims.Add(new Claim(DevelopmentSessionClaimType, _developmentAuthSession.Id));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    IssuedUtc = DateTimeOffset.UtcNow
                });
        }

        // GET: /Account/ForgotPassword
        // Muestra el formulario donde el usuario ingresa su correo.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("VwForgotPassword", new DtoForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword
        // Genera un token de recuperacion y envia el enlace por correo.
        // Siempre responde con pantalla de confirmacion para no revelar si el
        // correo existe o no en la base de datos.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ForgotPassword(DtoForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("VwForgotPassword", model);
            }

            var email = model.Email.Trim().ToLowerInvariant();
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Vigente == 1 && u.E_Mail != null && u.E_Mail.ToLower() == email);

            if (usuario != null)
            {
                _logger.LogInformation("Solicitud de recuperacion valida para el usuario {UsuarioId}", usuario.Id_Usuario);

                // Invalida tokens anteriores aun vigentes para que solo el
                // ultimo enlace de recuperacion sea util.
                var now = DateTime.UtcNow;
                var tokensPendientes = await _context.PasswordResetTokens
                    .Where(t => t.Id_Usuario == usuario.Id_Usuario &&
                                t.Fecha_Uso == null &&
                                t.Fecha_Expiracion > now)
                    .ToListAsync();

                foreach (var tokenPendiente in tokensPendientes)
                {
                    tokenPendiente.Fecha_Uso = now;
                }

                var token = CreateToken();

                // Solo se almacena el hash del token. El token real viaja una
                // sola vez en el enlace enviado al correo.
                _context.PasswordResetTokens.Add(new PasswordResetToken
                {
                    Id_Usuario = usuario.Id_Usuario,
                    TokenHash = HashToken(token),
                    Fecha_Creacion = now,
                    Fecha_Expiracion = now.AddMinutes(30),
                    Ip_Solicitud = GetClientIp()
                });

                await _context.SaveChangesAsync();

                // Url.Action construye el enlace absoluto al GET ResetPassword.
                var resetUrl = Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new { token },
                    Request.Scheme);

                if (!string.IsNullOrWhiteSpace(resetUrl))
                {
                    try
                    {
                        await _emailSender.SendPasswordResetAsync(usuario.E_Mail!, resetUrl);
                        _logger.LogInformation("Correo de recuperacion procesado para el usuario {UsuarioId}", usuario.Id_Usuario);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No fue posible enviar el correo de recuperacion para el usuario {UsuarioId}", usuario.Id_Usuario);
                    }
                }
            }
            else
            {
                _logger.LogInformation("Solicitud de recuperacion recibida para un correo no registrado o usuario inactivo");
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // GET: /Account/ForgotPasswordConfirmation
        // Confirmacion generica despues de solicitar recuperacion.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View("VwForgotPasswordConfirmation");
        }

        // GET: /Account/ResetPassword?token=...
        // Muestra el formulario para escribir una nueva contrasena.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string? token = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Login));
            }

            return View("VwResetPassword", new DtoResetPasswordViewModel { Token = token });
        }

        // POST: /Account/ResetPassword
        // Valida el token, actualiza la contrasena con BCrypt y marca el token
        // como usado para impedir reutilizacion.
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ResetPassword(DtoResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("VwResetPassword", model);
            }

            var tokenHash = HashToken(model.Token);
            var now = DateTime.UtcNow;

            var token = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t =>
                    t.TokenHash == tokenHash &&
                    t.Fecha_Uso == null &&
                    t.Fecha_Expiracion > now);

            if (token == null)
            {
                ModelState.AddModelError(string.Empty, "El enlace de recuperacion no es valido o ya expiro.");
                return View("VwResetPassword", model);
            }

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id_Usuario == token.Id_Usuario && u.Vigente == 1);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "El enlace de recuperacion no es valido o ya expiro.");
                return View("VwResetPassword", model);
            }

            usuario.Password = BCrypt.Net.BCrypt.HashPassword(model.Password, workFactor: 12);
            usuario.Debe_Cambiar_Password = 0;
            usuario.Fecha_Modifica = now;
            usuario.Maquina_Modifica = GetClientIp();
            token.Fecha_Uso = now;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        // GET: /Account/ResetPasswordConfirmation
        // Pantalla final luego de cambiar la contrasena correctamente.
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View("VwResetPasswordConfirmation");
        }

        // POST: /Account/Logout
        // Cierra la cookie de autenticacion y devuelve a la tienda publica.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("VwIndex", "Home");
        }

        // GET: /Account/AccessDenied
        // Vista mostrada cuando el usuario autenticado no tiene permisos.
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View("VwAccessDenied");
        }

        // Evita open redirect: solo permite volver a rutas locales del mismo sitio.
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("VwTiendas", "Tiendas");
        }

        private AuditContext GetAuditContext()
        {
            return new AuditContext(GetCurrentUserId(), GetClientIp());
        }

        private int? GetCurrentUserId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var usuarioId) ? usuarioId : null;
        }

        // Registra la IP que solicito o ejecuto una operacion sensible.
        private string? GetClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        // Genera un token aleatorio URL-safe para enviar por correo.
        private static string CreateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        // Convierte el token en SHA-256 hexadecimal para almacenarlo sin guardar
        // el secreto original en la base de datos.
        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }
    }
}

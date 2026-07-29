using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Tienda_Streaming.Business.Interfaces.Dominios;
using Tienda_Streaming.Business.Interfaces.CodigosPlataformas;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Business.Interfaces.ImagenesProducto;
using Tienda_Streaming.Business.Interfaces.Permisos;
using Tienda_Streaming.Business.Interfaces.RegistrarProductos;
using Tienda_Streaming.Business.Interfaces.RegistrarPublicaciones;
using Tienda_Streaming.Business.Interfaces.Roles;
using Tienda_Streaming.Business.Interfaces.RolesUser;
using Tienda_Streaming.Business.Interfaces.SistemaConfig;
using Tienda_Streaming.Business.Interfaces.Usuarios;
using Tienda_Streaming.Business.Services.Dominios;
using Tienda_Streaming.Business.Services.CodigosPlataformas;
using Tienda_Streaming.Business.Services.General;
using Tienda_Streaming.Business.Services.ImagenesProducto;
using Tienda_Streaming.Business.Services.Permisos;
using Tienda_Streaming.Business.Services.RegistrarProductos;
using Tienda_Streaming.Business.Services.RegistrarPublicaciones;
using Tienda_Streaming.Business.Services.Roles;
using Tienda_Streaming.Business.Services.RolesUser;
using Tienda_Streaming.Business.Services.SistemaConfig;
using Tienda_Streaming.Business.Services.Usuarios;
using Tienda_Streaming.Controllers;
using Tienda_Streaming.Data;
using Tienda_Streaming.Security;
using Tienda_Streaming.Services.Email;
using System.Security.Claims;
using System.Threading.RateLimiting;

// Permite que contenedores o scripts externos validen que la app puede arrancar
// sin abrir conexiones ni levantar todo el pipeline HTTP.
if (args.Contains("--healthcheck"))
{
    return;
}

// Crea el builder principal de ASP.NET Core. Desde este objeto se registran
// servicios, configuraciones y middlewares antes de iniciar la aplicacion.
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// En produccion las cookies deben viajar solo sobre HTTPS. Para pruebas locales
// en Docker sobre 127.0.0.1 se puede desactivar desde .env sin cambiar codigo.
var requireSecureCookies = builder.Configuration.GetValue("Security:RequireSecureCookies", true);
var secureCookiePolicy = requireSecureCookies
    ? CookieSecurePolicy.Always
    : CookieSecurePolicy.SameAsRequest;

// Permite ejecutar correctamente la aplicacion detras de un reverse proxy
// que termine HTTPS y envie X-Forwarded-Proto/X-Forwarded-For al contenedor.
var trustForwardedHeaders = builder.Configuration.GetValue<bool>("Security:TrustForwardedHeaders");
if (trustForwardedHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost;

        // En Docker el proxy suele estar en una red interna dinamica. Esta opcion
        // debe habilitarse solo cuando el contenedor no este expuesto directamente.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// -----------------------------
// Servicios
// -----------------------------

// Registra Entity Framework Core con PostgreSQL. La cadena real debe venir
// de User Secrets o variables de entorno, no de appsettings con contrasena.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("StrCuentasStreaming")));

// Carga la seccion "Smtp" para enviar correos de recuperacion y registra
// el servicio que encapsula MailKit.
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<CodigosPlataformasMailSettings>(builder.Configuration.GetSection("CodigosPlataformas"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();

// Registra servicios de negocio por flujo.
// Los controladores dependen de interfaces y no de Entity Framework directamente.
builder.Services.AddScoped<IGeneral, General>();
builder.Services.AddScoped<ICodigosPlataformas, CodigosPlataformasService>();
builder.Services.AddScoped<IRegistrarPublicaciones, RegistrarPublicacionesService>();
builder.Services.AddScoped<IRegistrarProductos, RegistrarProductosService>();
builder.Services.AddScoped<IImagenesProducto, ImagenesProductoService>();
builder.Services.AddScoped<IDominios, DominiosService>();
builder.Services.AddScoped<IPermisos, PermisosService>();
builder.Services.AddScoped<IPermisosMetodos, PermisosMetodosServices>();
builder.Services.AddScoped<IRoles, RolesService>();
builder.Services.AddScoped<IUsuarios, UsuariosService>();
builder.Services.AddScoped<IRolesUser, RolesUserService>();
builder.Services.AddScoped<ISistemaConfig, SistemaConfigService>();
builder.Services.AddScoped<MetodoPermisoFilter>();
var dataProtectionKeysPath = builder.Configuration["Security:DataProtectionKeysPath"]
    ?? Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data",
        "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("Tienda_Streaming")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddSingleton<ICuentaPasswordProtector, CuentaPasswordProtector>();
builder.Services.AddHostedService<CuentasPasswordEncryptionHostedService>();
builder.Services.AddHostedService<CodigosPlataformasHostedService>();
builder.Services.AddSingleton<DevelopmentAuthSession>();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 120 * 1024 * 1024;
    options.ValueLengthLimit = 1024 * 1024;
    options.MultipartHeadersLengthLimit = 16 * 1024;
});

// Configura antifalsificacion CSRF. El frontend envia este token en el header
// X-CSRF-TOKEN para POST, PUT y DELETE.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-TiendaStreaming.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = secureCookiePolicy;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Autenticacion por cookie para el login del proyecto. Las rutas configuradas
// apuntan a AccountController.Login, AccountController.Logout y AccessDenied.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Cookie.Name = "__Host-TiendaStreaming.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.Path = "/";
        options.Cookie.SecurePolicy = secureCookiePolicy;
        options.Cookie.SameSite = SameSiteMode.Strict;

        // En APIs no conviene redireccionar a HTML; se responde 401 para que
        // JavaScript decida si manda al usuario al login.
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        // Igual que login, los endpoints API devuelven 403 cuando hay sesion
        // pero no permisos suficientes.
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnValidatePrincipal = async context =>
        {
            if (!builder.Environment.IsDevelopment())
            {
                return;
            }

            var session = context.HttpContext.RequestServices.GetRequiredService<DevelopmentAuthSession>();
            var cookieSessionId = context.Principal?.FindFirstValue(AccountController.DevelopmentSessionClaimType);

            if (!string.Equals(cookieSessionId, session.Id, StringComparison.Ordinal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();

// Habilita MVC con controladores y vistas Razor. Se desactiva el Required
// implicito para que la validacion quede controlada por los atributos del modelo.
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<MetodoPermisoFilter>();
});

// Rate limiting para mitigar fuerza bruta en login/recuperacion y abuso general.
builder.Services.AddRateLimiter(options =>
{
    // Limite global por IP para reducir ruido o automatizaciones agresivas.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Politica generica disponible para endpoints futuros.
    options.AddFixedWindowLimiter("default", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
    });

    // Politica mas estricta para login, registro inicial y recuperacion.
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    // Endpoints publicos que consultan codigos, historial o ejecutan compra.
    options.AddFixedWindowLimiter("public-sensitive", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Construye la aplicacion con todos los servicios registrados.
var app = builder.Build();

// Ejecuta migraciones al iniciar cuando se habilita por configuracion o cuando
// el contenedor se invoca con --migrate. Esto facilita levantar una base nueva.
var ejecutarMigraciones = args.Contains("--migrate") ||
    app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

if (ejecutarMigraciones)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    if (args.Contains("--migrate"))
    {
        return;
    }
}

// -----------------------------
// Middleware
// -----------------------------

if (trustForwardedHeaders)
{
    app.UseForwardedHeaders();
}

// En produccion usa una vista de error controlada y HSTS para forzar HTTPS.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Encabezados de seguridad base. Reducen exposicion a clickjacking, sniffing,
// permisos de navegador innecesarios y ejecucion de recursos externos.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "0";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "media-src 'self'; " +
        "frame-src 'self' https://www.youtube.com https://www.youtube-nocookie.com; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";
    await next();
});

// Redirige HTTP a HTTPS antes de servir contenido.
app.UseHttpsRedirection();

// Sirve archivos estaticos de wwwroot: CSS, JS, imagenes, modelos 3D y librerias.
var staticFileContentTypes = new FileExtensionContentTypeProvider();
staticFileContentTypes.Mappings[".glb"] = "model/gltf-binary";
staticFileContentTypes.Mappings[".gltf"] = "model/gltf+json";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypes
});

// Activa el enrutamiento antes de rate limiting, autenticacion y autorizacion.
app.UseRouting();

// Aplica las politicas de rate limiting configuradas arriba.
app.UseRateLimiter();

// Primero identifica al usuario por cookie y luego evalua permisos.
app.UseAuthentication();
app.UseAuthorization();

// Si el usuario ingreso con una contrasena temporal, solo puede cambiarla o cerrar sesion.
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var debeCambiarPassword = context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim(AccountController.MustChangePasswordClaimType, "1");

    var rutaPermitida =
        path.StartsWithSegments("/Account/ChangePassword") ||
        path.StartsWithSegments("/Account/Logout") ||
        path.StartsWithSegments("/Account/AccessDenied") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/img") ||
        path == "/favicon.ico";

    if (debeCambiarPassword && !rutaPermitida)
    {
        if (path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                ok = false,
                mensaje = "Debes cambiar tu contrasena antes de continuar."
            });
            return;
        }

        context.Response.Redirect("/Account/ChangePassword");
        return;
    }

    await next();
});

// Endpoint publico para comprobar salud de la aplicacion.
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Mapea controladores API con atributos de ruta, por ejemplo /api/UsuariosApi.
app.MapControllers();

// Ruta MVC por defecto: Home/VwIndex cuando se entra a la raiz.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=VwIndex}/{id?}");

// Inicia el servidor web.
app.Run();

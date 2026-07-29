namespace Tienda_Streaming.Middleware
{
    // Middleware simple de diagnostico. Registra en consola la IP y la ruta
    // solicitada antes de pasar la peticion al siguiente middleware.
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        // ASP.NET Core inyecta el siguiente middleware del pipeline.
        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Metodo ejecutado por cada request HTTP.
        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path;
            var ip = context.Connection.RemoteIpAddress?.ToString();

            Console.WriteLine($"[{DateTime.UtcNow}] {ip} -> {path}");

            // Continua con el resto del pipeline.
            await _next(context);
        }
    }

    // Extension para poder registrar el middleware con app.UseRequestLogging().
    public static class RequestLoggingExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}

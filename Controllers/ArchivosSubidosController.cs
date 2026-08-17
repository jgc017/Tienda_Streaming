using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tienda_Streaming.Services.Storage;

namespace Tienda_Streaming.Controllers;

[AllowAnonymous]
[Route("uploads")]
public class ArchivosSubidosController : Controller
{
    private static readonly HashSet<string> CategoriasPermitidas = new(StringComparer.Ordinal)
    {
        "sistema", "inicio", "productos", "combos"
    };

    private readonly IAlmacenamientoArchivosSubidos _almacenamiento;

    public ArchivosSubidosController(IAlmacenamientoArchivosSubidos almacenamiento)
    {
        _almacenamiento = almacenamiento;
    }

    [HttpGet("{categoria}/{nombreArchivo}")]
    public async Task<IActionResult> Obtener(string categoria, string nombreArchivo)
    {
        if (!CategoriasPermitidas.Contains(categoria)
            || string.IsNullOrWhiteSpace(nombreArchivo)
            || nombreArchivo != Path.GetFileName(nombreArchivo))
        {
            return NotFound();
        }

        var archivo = await _almacenamiento.ObtenerAsync(categoria, nombreArchivo);
        if (archivo == null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        return File(archivo.Value.Contenido, archivo.Value.ContentType);
    }
}

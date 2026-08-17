using Microsoft.EntityFrameworkCore;
using Tienda_Streaming.Data;
using Tienda_Streaming.Models.Administracion;

namespace Tienda_Streaming.Services.Storage;

public class AlmacenamientoArchivosSubidos : IAlmacenamientoArchivosSubidos
{
    private readonly AppDbContext _context;

    public AlmacenamientoArchivosSubidos(AppDbContext context)
    {
        _context = context;
    }

    public async Task GuardarAsync(string categoria, string nombreArchivo, string contentType, Stream contenido)
    {
        await using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer);

        _context.ArchivosSubidos.Add(new ArchivoSubido
        {
            Categoria = categoria,
            NombreArchivo = nombreArchivo,
            ContentType = contentType,
            Contenido = buffer.ToArray(),
            Fecha_Creacion = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public Task<(byte[] Contenido, string ContentType)?> ObtenerAsync(string categoria, string nombreArchivo)
    {
        return _context.ArchivosSubidos
            .AsNoTracking()
            .Where(a => a.Categoria == categoria && a.NombreArchivo == nombreArchivo)
            .Select(a => new ValueTuple<byte[], string>?(
                new ValueTuple<byte[], string>(a.Contenido, a.ContentType)))
            .SingleOrDefaultAsync();
    }
}

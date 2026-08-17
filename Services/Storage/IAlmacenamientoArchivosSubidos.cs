namespace Tienda_Streaming.Services.Storage;

public interface IAlmacenamientoArchivosSubidos
{
    Task GuardarAsync(string categoria, string nombreArchivo, string contentType, Stream contenido);
    Task<(byte[] Contenido, string ContentType)?> ObtenerAsync(string categoria, string nombreArchivo);
}

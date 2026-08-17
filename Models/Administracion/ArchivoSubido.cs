namespace Tienda_Streaming.Models.Administracion;

// Archivo administrable almacenado en PostgreSQL. Evita depender del disco
// efimero de los contenedores de Render para las imagenes cargadas por usuarios.
public class ArchivoSubido
{
    public int Id_ArchivoSubido { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Contenido { get; set; } = Array.Empty<byte>();
    public DateTime Fecha_Creacion { get; set; }
}

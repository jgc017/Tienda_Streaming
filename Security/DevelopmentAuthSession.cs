namespace Tienda_Streaming.Security
{
    // Identificador en memoria para invalidar cookies al reiniciar la app en desarrollo.
    public sealed class DevelopmentAuthSession
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }
}

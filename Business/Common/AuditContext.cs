namespace Tienda_Streaming.Business.Common
{
    // Datos de auditoria calculados en el controlador desde la sesion y la peticion HTTP.
    // Los servicios los reciben para llenar Id_Usuario_Crea/Modifica y Maquina_Creacion/Modifica.
    public sealed record AuditContext(int? UserId, string? Machine);
}

namespace Tienda_Streaming.Models.Dto.General
{
    // Modelo usado por Views/Shared/Error.cshtml.
    // Expone el identificador de la solicitud para diagnostico sin mostrar detalles sensibles.
    public class DtoErrorViewModel
    {
        // Identificador correlacionable en logs.
        public string? RequestId { get; set; }

        // La vista decide mostrar el RequestId solo cuando existe.
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

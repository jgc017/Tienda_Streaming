namespace Tienda_Streaming.Business.Common
{
    // Resultado uniforme para la capa de negocio.
    // El servicio decide si la operacion fue exitosa y que codigo HTTP corresponde,
    // mientras el controlador se limita a convertirlo en una respuesta de API.
    public sealed class ServiceResult
    {
        public bool Ok { get; private init; }
        public int StatusCode { get; private init; }
        public string? Mensaje { get; private init; }
        public object? Data { get; private init; }
        public IReadOnlyDictionary<string, object?>? Extras { get; private init; }
        public string? AuditDescription { get; private init; }

        public static ServiceResult Success(
            string? mensaje = null,
            object? data = null,
            int statusCode = StatusCodes.Status200OK,
            IReadOnlyDictionary<string, object?>? extras = null,
            string? auditDescription = null)
        {
            return new ServiceResult
            {
                Ok = true,
                StatusCode = statusCode,
                Mensaje = mensaje,
                Data = data,
                Extras = extras,
                AuditDescription = auditDescription
            };
        }

        public static ServiceResult Fail(int statusCode, string mensaje, object? data = null)
        {
            return new ServiceResult
            {
                Ok = false,
                StatusCode = statusCode,
                Mensaje = mensaje,
                Data = data
            };
        }

        // Devuelve un objeto serializable con la misma forma que ya consumen los JavaScript:
        // { ok, mensaje, data } y, cuando aplica, valores extra como id_Dominio.
        public Dictionary<string, object?> ToApiResponse()
        {
            var response = new Dictionary<string, object?>
            {
                ["ok"] = Ok
            };

            if (!string.IsNullOrWhiteSpace(Mensaje))
            {
                response["mensaje"] = Mensaje;
            }

            if (Data != null)
            {
                response["data"] = Data;
            }

            if (Extras != null)
            {
                foreach (var item in Extras)
                {
                    response[item.Key] = item.Value;
                }
            }

            return response;
        }
    }
}

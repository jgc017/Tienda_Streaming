using System.ComponentModel.DataAnnotations;

namespace Tienda_Streaming.Models.Dto.Administracion.Permisos
{
    // DTO recibido por PUT /api/PermisosMetodosApi/P_UdpPermisoMetodo/{id}.
    // Los datos tecnicos del metodo no se editan manualmente porque vienen
    // de la sincronizacion automatica de controladores.
    public class DtoPermisoMetodoUpdateRequest
    {
        [StringLength(200)]
        public string? Descripcion { get; set; }

        [Range(0, 1)]
        public short Vigente { get; set; } = 1;
    }
}

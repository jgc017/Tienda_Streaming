using Tienda_Streaming.Models.Dto.General;
using Tienda_Streaming.Business.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tienda_Streaming.Business.Interfaces.General
{
    // Contrato para consultas reutilizables por varias pantallas del proyecto.
    // No debe contener reglas propias de un CRUD especifico.
    public interface IGeneral
    {
        Task<List<DtoDominioDropdownItem>> ObtenerDominios();
        Task<List<DtoDominioDropdownItem>> ObtenerDominiosPorPadre(int idDominio, int? idSubDominio = null);
        Task<List<DtoUsuarioDropdownItem>> ObtenerUsuarios();
        Task<List<DtoMenuDropdownItem>> ObtenerMenus();
        Task<List<DtoMenuSistemaItem>> F_GetMenu(IEnumerable<int> rolesUsuario);
        Task<bool> TienePermisoMenu(IEnumerable<int> rolesUsuario, string controlador, string vista);
        Task<bool> TienePermisoMetodo(IEnumerable<int> rolesUsuario, string controlador, string metodo, string httpMetodo);
        Task RegistrarAuditoria(AuditContext audit, string formulario, string metodoEjecutado, string descripcion);
    }
}

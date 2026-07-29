using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Tienda_Streaming.Business.Interfaces.General;
using Tienda_Streaming.Controllers;

namespace Tienda_Streaming.Security
{
    // Filtro global para proteger metodos API con permisos configurados por rol.
    // Solo aplica a controladores bajo Controllers.Api; las vistas siguen usando
    // los permisos de menu existentes.
    public class MetodoPermisoFilter : IAsyncActionFilter
    {
        private readonly IGeneral _general;

        public MetodoPermisoFilter(IGeneral general)
        {
            _general = general;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor action ||
                !EsControladorApi(action) ||
                TieneAllowAnonymous(action) ||
                EsMetodoEstandar(action.ActionName))
            {
                await next();
                return;
            }

            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            var roles = context.HttpContext.User
                .FindAll(AccountController.RoleIdClaimType)
                .Select(c => int.TryParse(c.Value, out var idRol) ? idRol : 0)
                .Where(idRol => idRol > 0)
                .ToList();

            var permitido = await _general.TienePermisoMetodo(
                roles,
                action.ControllerName,
                action.ActionName,
                context.HttpContext.Request.Method);

            if (permitido)
            {
                await next();
                return;
            }

            context.Result = new JsonResult(new
            {
                ok = false,
                mensaje = "Usted no tiene permisos para realizar esta accion"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        private static bool EsControladorApi(ControllerActionDescriptor action)
        {
            return (action.ControllerTypeInfo.Namespace ?? string.Empty)
                .Contains(".Controllers.Api", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TieneAllowAnonymous(ControllerActionDescriptor action)
        {
            return action.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any() ||
                   action.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any();
        }

        private static bool EsMetodoEstandar(string metodo)
        {
            return metodo.EndsWith("List", StringComparison.OrdinalIgnoreCase) ||
                   metodo.Contains("Dropdown", StringComparison.OrdinalIgnoreCase);
        }
    }
}

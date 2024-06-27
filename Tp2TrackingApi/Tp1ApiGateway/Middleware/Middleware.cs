using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using WebGateway.Controllers;
using static WebGateway.Controllers.AuthenticationFilterService; // Importa la clase AuthenticationFilterService

namespace Tp1ApiGateway.Middleware
{
  
    public class AuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _loginPath = "/AuthenticationFilterService/login";
        public AuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Verificar si la solicitud es para el endpoint de login y permitir pedir el token
            if (context.Request.Path.Equals(_loginPath, StringComparison.OrdinalIgnoreCase) &&
                context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Extraer el token del encabezado de autorización
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (token != null)
            {
                // Verificar la validez del token
                var tokenInfo = AuthenticationFilterService.GetTokenInfo(token);
                if (tokenInfo != null)
                {
                    // Verificar si el usuario tiene el rol de administrador
                    var userRole = tokenInfo.UserRole;
                    if (context.Request.Path.Equals("/Users", StringComparison.OrdinalIgnoreCase) &&
                        context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                        userRole != "ADMIN")
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        await context.Response.WriteAsync("Forbidden: Only admins can access this endpoint");
                        return;
                    }

                    // Permitir acceso al endpoint /api/trackingService/event para todos los usuarios
                    if (context.Request.Path.Equals("/api/trackingService/event", StringComparison.OrdinalIgnoreCase) &&
                        context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        await _next(context);
                        return;
                    }
                    await _next(context);
                    return;
                }
            }

            // Devolver error de autorización si el token no es válido o no se proporciona
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
        }
    }
    public static class AuthorizationMiddlewareExtension
    {
        public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder builder) 
        {
            return builder.UseMiddleware<AuthorizationMiddleware>();
        }
    }
}

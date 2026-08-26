
using Crud.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class RequisitoAcessoHandler : AuthorizationHandler<RequisitoAcesso>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequisitoAcessoHandler(IServiceProvider serviceProvider, IHttpContextAccessor httpContextAccessor)
    {
        _serviceProvider = serviceProvider;
         _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RequisitoAcesso requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        string rotaSolicitada = string.Empty;
        string metodoHttp = string.Empty;

        if (httpContext != null)
        {            
            metodoHttp = httpContext.Request.Method;
          
            var endpointData = httpContext.GetEndpoint();
            var routePatternProvider = endpointData?.Metadata.GetMetadata<Microsoft.AspNetCore.Routing.Patterns.RoutePattern>();            
          
            rotaSolicitada = routePatternProvider != null 
                ? $"/{routePatternProvider.RawText?.TrimStart('/')}" 
                : httpContext.Request.Path.Value ?? string.Empty;
        }
                
        var usuarioIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;       
        var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
       
        if (string.IsNullOrEmpty(usuarioIdClaim) || 
            !Guid.TryParse(usuarioIdClaim, out var usuarioId) || 
            !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Fail(); 
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrudContext>();

        var usuarioAutorizado = await db.Usuarios
            .Include(u => u.Perfil) 
            .Include(u => u.Tenant) 
            .Where(u => u.Id == usuarioId 
                    && u.TenantId == tenantId 
                    && u.Tenant.Ativo == "S"
                    && u.Ativo == "S")
            .Select(u => new 
            {  
                EhAdminGeral = u.Perfil != null && u.Perfil.Nome == "Administrador Geral",
                EhMaster = u.Master == true,
                PerfilIdOperador = u.PerfilId
            })
            .FirstOrDefaultAsync();


        if (usuarioAutorizado != null && (usuarioAutorizado.EhAdminGeral || usuarioAutorizado.EhMaster))
        {
            context.Succeed(requirement);
        } 
        else if (usuarioAutorizado != null)
        {           
            var possuiPermissaoMapeada = await db.NiveisAcessos
                .AnyAsync(n => n.PefilId == usuarioAutorizado.PerfilIdOperador 
                            && n.TenantId == tenantId
                            && n.Rota.Rota == rotaSolicitada 
                            && n.Rota.Metodo == metodoHttp);

            if (possuiPermissaoMapeada)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
        else
        {
            context.Fail();
        }
    }

}

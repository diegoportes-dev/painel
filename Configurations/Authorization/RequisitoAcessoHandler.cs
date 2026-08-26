
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
        // Obtém o HttpContext atual
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext != null)
        {
            // 1. Obtém o Endpoint metadata completo (onde você pode ler a rota, atributos como [Authorize], etc)
            var endpoint = httpContext.GetEndpoint();
            
            // 2. Obtém a Rota em formato de string (Ex: "/api/usuarios/criar")
            var rotaSolicitada = httpContext.Request.Path.Value;
            
            // 3. Obtém o Método HTTP (Ex: "POST", "GET")
            var metodoHttp = httpContext.Request.Method;

            // EXEMPLO DE USO: Se você quiser saber o nome amigável do Endpoint:
            var nomeEndpoint = endpoint?.DisplayName; 
        }
        
        // 1. Extrai as informações de identificação gravadas no Token JWT
        var usuarioIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;

        // Valida se os dados básicos existem no token antes de ir ao banco
        if (string.IsNullOrEmpty(usuarioIdClaim) || 
            !Guid.TryParse(usuarioIdClaim, out var usuarioId) || 
            !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            context.Fail(); // Bloqueia se o token estiver corrompido ou incompleto
            return;
        }

        // 2. Criamos o escopo do banco de dados de forma segura
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrudContext>();

        // 3. 🔍 SUA NOVA QUERY: 
        // Busca o usuário no banco, valida o Tenant e checa se o Perfil dele é o Admin Geral
        var usuarioAutorizado = await db.Usuarios
            .Include(u => u.Perfil) // Inclui o relacionamento de perfil se necessário
            .Include(u => u.Tenant) // Inclui o relacionamento de tenant
            .Where(u => u.Id == usuarioId 
                     && u.TenantId == tenantId 
                     && u.Tenant.Ativo == "S") // Garante que o Tenant está ativo ("S")
            .Select(u => new 
            {  
                EhAdminGeral = u.Perfil != null && u.Perfil.Nome == "Administrador Geral"
            })
            .FirstOrDefaultAsync();

        // 4. Validação final do resultado da query
        if (usuarioAutorizado != null && usuarioAutorizado.EhAdminGeral)
        {
            context.Succeed(requirement); // 🔓 Autoriza o acesso! Tudo OK no banco.
        }
        else
        {
            context.Fail(); // 🔒 Bloqueia o acesso (Retorna o seu 403 unificado)
        }
    }
}

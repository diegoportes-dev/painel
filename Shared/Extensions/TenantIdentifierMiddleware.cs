using System.Security.Claims;
using Crud.Data;
using Microsoft.EntityFrameworkCore;

public class TenantIdentifierMiddleware
{
    private readonly RequestDelegate _next;

    public TenantIdentifierMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, CrudContext dbCentral)
    {
        // 1. Verifica se o usuário atual está autenticado na API
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // 2. Extrai o ID do usuário autenticado (NameIdentifier do JWT)
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                // 3. Consulta rápida no banco central para pegar o DatabaseName do Tenant deste usuário
                var databaseName = await dbCentral.Usuarios
                    .Where(u => u.Id == userId)
                    .Select(u => u.Tenant!.DatabaseName)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(databaseName))
                {
                    // 4. Injeta a String de Conexão contextualizada do arquivo SQLite do cliente
                    var connectionString = $"Data Source={databaseName}";
                    tenantProvider.SetConnectionString(connectionString);
                }
            }
        }

        // Continua o fluxo da requisição para os endpoints da API Minimal
        await _next(context);
    }
}

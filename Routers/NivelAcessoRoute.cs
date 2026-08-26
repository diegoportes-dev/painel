using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Crud.Data;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Security.Claims;

public sealed record NivelAcessoCollectionResponse(IReadOnlyList<NivelAcessoOutputDto> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);
public sealed record NivelAcessoResourceResponse(NivelAcessoOutputDto Data, IReadOnlyList<HyperLink> Links);
public sealed record NivelAcessoRotaResourceResponse(IReadOnlyList<NivelAcessoRotaOutputDto> Data);


public static class NivelAcessoRoute
{    
    public static void MapNivelAcessoRoutes(this WebApplication app)
    {
        string prefixo = "niveis-acesso";
        var route = app.MapGroup($"/{prefixo}");
        
        // 1. GET ALL (Paginado com Isolamento de Tenant)
        route.MapGet("", 
            async Task<IResult> (
                int? page, 
                int? pageSize, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var pageNumber = page is null or < 1 ? 1 : page.Value;
                var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;

                // Extrai o ID do Usuário operador do Token JWT
                var logadoIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(logadoIdClaim) || !Guid.TryParse(logadoIdClaim, out var logadoUserId))
                {
                    return Results.Json(new { message = "Usuário operador não identificado ou token inválido." }, statusCode: 401);
                }

                // Consulta rápida para extrair privilégios e TenantId do operador
                var operadorInfo = await db.Usuarios
                    .Where(u => u.Id == logadoUserId)
                    .Select(u => new { u.TenantId, IsMaster = u.Master })
                    .FirstOrDefaultAsync();

                if (operadorInfo == null)
                {
                    return Results.Json(new { message = "Operador não localizado no catálogo." }, statusCode: 401);
                }

                Guid? operadorTenantId = operadorInfo.TenantId;
                bool ehMaster = operadorInfo.IsMaster ?? false;

                // Inicializa a Query aplicando as regras de isolamento lógico por Tenant
                var queryBase = db.NiveisAcessos
                    .Include(n => n.Perfil)
                    .Include(n => n.Rota)
                    .AsQueryable();
                
                if (!ehMaster && operadorTenantId.HasValue)
                {
                    // Restringe estritamente aos registros pertencentes ao Tenant do usuário conectado
                    queryBase = queryBase.Where(n => n.TenantId == operadorTenantId.Value);
                }

                var totalItems = await queryBase.CountAsync();
                var totalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

                var niveisAcesso = await queryBase
                    .OrderBy(n => n.Perfil.Nome)
                    .ThenBy(n => n.Rota.Rota)
                    .Skip((pageNumber - 1) * requestedPageSize)
                    .Take(requestedPageSize)
                    .Select(n => new NivelAcessoOutputDto(n))
                    .ToListAsync();

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var pageQuery = $"?page={pageNumber}&pageSize={requestedPageSize}";

                var links = new List<HyperLink>
                {
                    new("self", $"{baseUrl}/{prefixo}{pageQuery}", "GET"),
                    new("collection", $"{baseUrl}/{prefixo}", "GET"),
                    new("create", $"{baseUrl}/{prefixo}", "POST")
                };

                if (pageNumber > 1)
                {
                    links.Add(new HyperLink("prev", $"{baseUrl}/{prefixo}?page={pageNumber - 1}&pageSize={requestedPageSize}", "GET"));
                }

                if (pageNumber < totalPages)
                {
                    links.Add(new HyperLink("next", $"{baseUrl}/{prefixo}?page={pageNumber + 1}&pageSize={requestedPageSize}", "GET"));
                }

                if (totalPages > 0)
                {
                    links.Add(new HyperLink("first", $"{baseUrl}/{prefixo}?page=1&pageSize={requestedPageSize}", "GET"));
                    links.Add(new HyperLink("last", $"{baseUrl}/{prefixo}?page={totalPages}&pageSize={requestedPageSize}", "GET"));
                }

                return TypedResults.Ok(new NivelAcessoCollectionResponse(
                    niveisAcesso,
                    new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                    links));
            }
        ).RequireAuthorization();

        // 2. POST (Criação herdando dinamicamente o TenantId do Operador)
        route.MapPost("", 
            async Task<IResult> (
                NivelAcessoInputPostDTO input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<NivelAcessoInputPostDTO> validator) => 
            {   
                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = validationResult.ToDictionary() });
                }

                var logadoIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(logadoIdClaim) || !Guid.TryParse(logadoIdClaim, out var logadoUserId))
                {
                    return Results.Json(new { message = "Usuário operador não identificado ou token inválido." }, statusCode: 401);
                }

                var operadorInfo = await db.Usuarios
                    .Where(u => u.Id == logadoUserId)
                    .Select(u => new { u.TenantId })
                    .FirstOrDefaultAsync();

                if (operadorInfo == null)
                {
                    return Results.Json(new { message = "Operador não localizado no catálogo." }, statusCode: 401);
                }

                if (!operadorInfo.TenantId.HasValue)
                {
                    return Results.Json(new { message = "Operador comum sem empresa vinculada.", statusCode = 403 });
                }
                
                // Mapeamento manual obedecendo às propriedades da entidade fornecida
                var nivelAcesso = new NivelAcessoModel
                {
                    Id = Guid.NewGuid(),
                    PefilId = input.PerfilId,
                    RotaId = input.RotaId,
                    TenantId = operadorInfo.TenantId.Value
                };

                db.NiveisAcessos.Add(nivelAcesso);
                await db.SaveChangesAsync();

                // Recarrega referências completas para o DTO de output se necessário
                nivelAcesso.Perfil = await db.Perfis.FindAsync(nivelAcesso.PefilId);
                nivelAcesso.Rota = await db.Rotas.FindAsync(nivelAcesso.RotaId);

                List<HyperLink> links = Links.GenerateLinks(httpContext, nivelAcesso.Id, prefixo);

                return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{nivelAcesso.Id}", 
                    new NivelAcessoResourceResponse(new NivelAcessoOutputDto(nivelAcesso), links));
            }
        ).RequireAuthorization();

        // 3. GET BY ID
        route.MapGet("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var nivelAcesso = await db.NiveisAcessos
                    .Include(n => n.Perfil)
                    .Include(n => n.Rota)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (nivelAcesso is null)
                {
                    return TypedResults.NotFound();
                }

                List<HyperLink> links = Links.GenerateLinks(httpContext, nivelAcesso.Id, prefixo);
                
                return TypedResults.Ok(new NivelAcessoResourceResponse(new NivelAcessoOutputDto(nivelAcesso), links));
            }
        ).RequireAuthorization();

        // 4. PUT
        route.MapPut("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                NivelAcessoInputPutDTO input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<(Guid id, NivelAcessoInputPutDTO input)> validator) =>
            {
                var validationResult = await validator.ValidateAsync((id, input));
                if (!validationResult.IsValid)
                {
                    var registroNaoEncontrado = validationResult.Errors.FirstOrDefault(e => e.ErrorCode == "NotFound");
                    if (registroNaoEncontrado is not null)
                    {
                        return Results.NotFound(new { message = registroNaoEncontrado.ErrorMessage });
                    }

                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = validationResult.ToDictionary() });
                }

                var nivelAcesso = await db.NiveisAcessos.FindAsync(id);
                
                // Atualização dos campos de relacionamento do Model
                nivelAcesso.PefilId = input.PerfilId;
                nivelAcesso.RotaId = input.RotaId;

                await db.SaveChangesAsync();

                nivelAcesso.Perfil = await db.Perfis.FindAsync(nivelAcesso.PefilId);
                nivelAcesso.Rota = await db.Rotas.FindAsync(nivelAcesso.RotaId);

                List<HyperLink> links = Links.GenerateLinks(httpContext, nivelAcesso.Id, prefixo);

        return TypedResults.Ok(new NivelAcessoResourceResponse(new NivelAcessoOutputDto(nivelAcesso), links));
        }).RequireAuthorization();

        // 5. DELETE
        route.MapDelete("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db, 
                IValidator<Guid> validator) =>
            {
                var nivelAcesso = await db.NiveisAcessos.FindAsync(id);
                if (nivelAcesso is null)
                {
                    return TypedResults.NotFound(new { message = $"Nível de acesso não encontrado." });
                }

                db.NiveisAcessos.Remove(nivelAcesso);
                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            }
        ).RequireAuthorization();



        // Adicione junto com os outros mapeamentos do sistema
        route.MapGet("/rotas", async (CrudContext db) =>
        {
            var response = await db.Rotas
                .OrderBy(r => r.Menu)
                .ThenBy(r => r.Rota)
                .Select(r => new NivelAcessoRotaOutputDto(r)) 
                .ToListAsync();

            // 🔓 Agora o tipo bate perfeitamente com a assinatura do record!
            return TypedResults.Ok(new NivelAcessoRotaResourceResponse(response));
        }).RequireAuthorization();

        route.MapPost("/matriz", 
        async (
            NivelAcessoMatrizAcessoInputDto input, 
            CrudContext db, 
            HttpContext httpContext) =>
        {
            if (input.PerfilId == Guid.Empty)
                return Results.BadRequest(new { message = "O Perfil é obrigatório." });

            // Captura o TenantId do operador logado para manter o isolamento lógico
            var tenantIdClaim = httpContext.User.FindFirst("TenantId")?.Value;
            if (!Guid.TryParse(tenantIdClaim, out var tenantId))
                return Results.Json(new { message = "Tenant não identificado no token." }, statusCode: 401);

            // Inicia uma transação para garantir atomicidade (deleta e insere tudo ou nada)
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {               
                var permissoesAntigas = await db.NiveisAcessos
                    .Where(n => n.PefilId == input.PerfilId && n.TenantId == tenantId)
                    .ToListAsync();

                db.NiveisAcessos.RemoveRange(permissoesAntigas);

                // 2. Insere as novas rotas selecionadas nos checkboxes
                if (input.RotaIds != null && input.RotaIds.Count > 0)
                {
                    var novosNiveis = input.RotaIds.Select(rotaId => new NivelAcessoModel
                    {
                        Id = Guid.NewGuid(),
                        PefilId = input.PerfilId,
                        RotaId = rotaId,
                        TenantId = tenantId
                    });

                    await db.NiveisAcessos.AddRangeAsync(novosNiveis);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return Results.Ok(new { message = "Matriz de acessos atualizada com sucesso!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Results.Json(new { message = $"Erro ao gravar matriz: {ex.Message}" }, statusCode: 500);
            }
        }).RequireAuthorization();

    }
}
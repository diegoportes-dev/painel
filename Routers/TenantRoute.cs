using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Crud.Data; // Ajuste para o namespace correto do seu CrudContext
using Microsoft.EntityFrameworkCore;
using FluentValidation;

// Records de resposta padronizados seguindo o seu modelo do Perfil
public sealed record TenantCollectionResponse(IReadOnlyList<TenantOutputDto> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);
public sealed record TenantResourceResponse(TenantOutputDto Data, IReadOnlyList<HyperLink> Links);

public static class TenantRoute
{    
    public static void MapTenantRoutes(this WebApplication app)
    {
        string prefixo = "tenants";
        var route = app.MapGroup($"/{prefixo}");

        // 1. GET ALL (Paginação e Links HATEOAS)
        route.MapGet("", 
            async Task<IResult> (
                int? page, 
                int? pageSize, 
                CrudContext db, // Injeta o banco do catálogo/central
                HttpContext httpContext) =>
            {
                var pageNumber = page is null or < 1 ? 1 : page.Value;
                var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;

                var totalItems = await db.Tenants.CountAsync();
                var totalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

                var tenants = await db.Tenants
                    .OrderBy(t => t.Nome)
                    .Skip((pageNumber - 1) * requestedPageSize)
                    .Take(requestedPageSize)
                    .Select(t => new TenantOutputDto(t))
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

                return TypedResults.Ok(new TenantCollectionResponse(
                    tenants,
                    new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                    links));
            }
        ).RequireAuthorization();

        // 2. POST (Criação de Tenant com validação)
        route.MapPost("", 
            async Task<IResult> (
                TenantInputPostDto input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<TenantInputPostDto> validator) => 
            {   
                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }
                
                // Instancia o modelo limpando caracteres do documento
                var tenant = new TenantModel
                {
                    Id = Guid.NewGuid(),
                    Nome = input.Nome,
                    NomeSecundario = input.NomeSecundario,
                    Documento = new string(input.Documento.Where(char.IsDigit).ToArray()),
                    Tipo = input.Tipo,
                    Slug = input.Slug.ToLower().Trim(),
                    // Montagem dinâmica da String de Conexão isolada baseada no Slug
                    ConnectionString = $"Server=seu_servidor;Database=db_tenant_{input.Slug.ToLower().Trim()};User Id=sa;Password=sua_senha;TrustServerCertificate=True;"
                };

                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, tenant.Id, prefixo);

                return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{tenant.Id}", 
                    new TenantResourceResponse(new TenantOutputDto(tenant), links));
            }
        ).RequireAuthorization();

        // 3. GET BY ID
        route.MapGet("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var tenant = await db.Tenants.FindAsync(id);
                if (tenant is null)
                {
                    return TypedResults.NotFound();
                }

                List<HyperLink> links = Links.GenerateLinks(httpContext, tenant.Id, prefixo);
                
                return TypedResults.Ok(new TenantResourceResponse(new TenantOutputDto(tenant), links));
            }
        ).RequireAuthorization();

        // 4. PUT (Atualização completa)
        route.MapPut("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                TenantInputPutDto input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<(Guid id, TenantInputPutDto input)> validator) =>
            {
                var validationResult = await validator.ValidateAsync((id, input));
                if (!validationResult.IsValid)
                {
                    var tenantNaoEncontrado = validationResult.Errors
                        .FirstOrDefault(e => e.ErrorCode == "NotFound");

                    if (tenantNaoEncontrado is not null)
                    {
                        return Results.NotFound(new { message = tenantNaoEncontrado.ErrorMessage });
                    }

                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

                var tenant = await db.Tenants.FindAsync(id);
                
                // Atualização dos campos do Tenant
                tenant.Nome = input.Nome;
                tenant.NomeSecundario = input.NomeSecundario;
                tenant.Documento = new string(input.Documento.Where(char.IsDigit).ToArray());
                tenant.Tipo = input.Tipo;
                tenant.Slug = input.Slug.ToLower().Trim();
                tenant.Ativo = input.Ativo; // Herdado de AuditoriaModel caso aplicável

                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, tenant.Id, prefixo);

                return TypedResults.Ok(new TenantResourceResponse(new TenantOutputDto(tenant), links));
            }
        ).RequireAuthorization();

        // 5. DELETE
        route.MapDelete("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db) =>
            {
                var tenant = await db.Tenants.FindAsync(id);
                if (tenant is null)
                {
                    return TypedResults.NotFound(new { message = $"Tenant com ID {id} não encontrado." });
                }

                db.Tenants.Remove(tenant);
                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            }
        ).RequireAuthorization();
    }
}

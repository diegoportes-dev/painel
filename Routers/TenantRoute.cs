using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Crud.Data; 
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace Crud.Routers;

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
                CrudContext db, 
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
        );

        // 2. POST (Criação de Tenant com Geração Automática de Slug)
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

                // Geração e garantia de unicidade do Slug de forma automatizada
                var slugBase = SlugHelper.GerarSlug(input.Nome);
                var slugFinal = slugBase;
                int contador = 1;

                while (await db.Tenants.AnyAsync(t => t.Slug == slugFinal))
                {
                    slugFinal = $"{slugBase}_{contador}";
                    contador++;
                }
                
                var tenant = new TenantModel
                {
                    Id = Guid.NewGuid(),
                    Nome = input.Nome,
                    NomeSecundario = input.NomeSecundario,
                    Documento = new string(input.Documento.Where(char.IsDigit).ToArray()),
                    Tipo = input.Tipo,
                    Slug = slugFinal, // Definido automaticamente e em definitivo
                    DatabaseName = $"db_{slugFinal}",
                    
                    // TODO: Substituir pela chamada do seu ICriptografiaService real futuramente
                    DbUserEncrypted = $"encrypted_user_{slugFinal}",
                    DbPasswordEncrypted = $"encrypted_pwd_{Guid.NewGuid().ToString("N").Substring(0, 10)}"
                };

                db.Tenants.Add(tenant);
                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, tenant.Id, prefixo);

                return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{tenant.Id}", 
                    new TenantResourceResponse(new TenantOutputDto(tenant), links));
            }
        );

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
        );

        // 4. PUT (Atualização cadastral sem alteração de infraestrutura)
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
                
                // Atualização estritamente dos campos comerciais e cadastrais permitidos
                tenant.Nome = input.Nome;
                tenant.NomeSecundario = input.NomeSecundario;
                tenant.Documento = new string(input.Documento.Where(char.IsDigit).ToArray());
                tenant.Tipo = input.Tipo;
                tenant.Ativo = input.Ativo ?? tenant.Ativo; 

                // ATENÇÃO: Slug, DatabaseName, Usuário e Senha permanecem totalmente protegidos e inalterados

                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, tenant.Id, prefixo);

                return TypedResults.Ok(new TenantResourceResponse(new TenantOutputDto(tenant), links));
            }
        );

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
        );

        route.MapPatch("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                TenantInputPatchDto input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<(Guid id, TenantInputPatchDto input)> validator) =>
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
                    
                    // Aplica atualizações parciais de forma segura (Apenas se o campo foi enviado)
                    if (input.Nome != null) tenant!.Nome = input.Nome;
                    if (input.NomeSecundario != null) tenant!.NomeSecundario = input.NomeSecundario;
                    if (input.Tipo != null) tenant!.Tipo = input.Tipo.Value;
                    if (input.Ativo != null) tenant!.Ativo = input.Ativo;
                    
                    if (input.Documento != null) 
                    {
                        tenant!.Documento = new string(input.Documento.Where(char.IsDigit).ToArray());
                    }

                    // BLINDADO: Slug, DatabaseName, DbUserEncrypted e DbPasswordEncrypted nunca serão alterados aqui!

                    await db.SaveChangesAsync();

                    List<HyperLink> links = Links.GenerateLinks(httpContext, tenant!.Id, prefixo);

                    return TypedResults.Ok(new TenantResourceResponse(new TenantOutputDto(tenant), links));
                }
            );
    }
}

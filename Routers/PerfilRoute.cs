using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Crud.Data;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

public sealed record PerfilCollectionResponse(IReadOnlyList<PerfilOutputDto> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);
public sealed record PerfilResourceResponse(PerfilOutputDto Data, IReadOnlyList<HyperLink> Links);

public static class PerfilRoute
{    
    public static void MapPerfilRoutes(this WebApplication app)
    {
        string prefixo = "perfis";
        var route = app.MapGroup($"/{prefixo}");

        route.MapGet("", 
            async Task<IResult> (
                int? page, 
                int? pageSize, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var pageNumber = page is null or < 1 ? 1 : page.Value;
                var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;

                var totalItems = await db.Perfis.CountAsync();
                var totalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

                var perfis = await db.Perfis
                    .OrderBy(p => p.Nome)
                    .Skip((pageNumber - 1) * requestedPageSize)
                    .Take(requestedPageSize)
                    .Select(p => new PerfilOutputDto(p))
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

                return TypedResults.Ok(new PerfilCollectionResponse(
                    perfis,
                    new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                    links));
            }
        ).RequireAuthorization();

        route.MapPost("", 
            async Task<IResult> (
                PerfilInputPostDTO input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<PerfilInputPostDTO> validator) => 
            {   

                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    // Formata os erros em um dicionário amigável (Propriedade -> Mensagens de erro)
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }
                
                var perfil = new PerfilModel(input);

                db.Perfis.Add(perfil);
                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);

                return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{perfil.Id}", new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
                                              
            }
        ).RequireAuthorization();

        route.MapGet("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var perfil = await db.Perfis.FindAsync(id);
                if (perfil is null)
                {
                    return TypedResults.NotFound();
                }

                List<HyperLink> links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);
                
                return TypedResults.Ok(new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
            }
        ).RequireAuthorization();

        route.MapPut("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                PerfilInputPutDTO input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<(Guid id, PerfilInputPutDTO input)>validator) =>
            {

                var validationResult = await validator.ValidateAsync((id, input));
                if (!validationResult.IsValid)
                {
                    var usuarioNaoEncontrado = validationResult.Errors
                        .FirstOrDefault(e => e.ErrorCode == "NotFound");

                    if (usuarioNaoEncontrado is not null)
                    {
                        // Se o usuário não existe, retorna 404 com a mensagem do FluentValidation
                        return Results.NotFound(new { message = usuarioNaoEncontrado.ErrorMessage });
                    }

                    // 2. Se caiu aqui, o usuário existe, mas há erros de dados (ex: e-mail em uso) -> Retorna 400
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

                var perfil = await db.Perfis.FindAsync(id);
                perfil.UpdatePerfil(input);

                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);

                return TypedResults.Ok(new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
                   
            }
        ).RequireAuthorization();

        route.MapDelete("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db) =>
            {
                var perfil = await db.Perfis.FindAsync(id);
                if (perfil is null)
                {
                    return TypedResults.NotFound(new { message = $"Perfil com ID {id} não encontrado." });
                }

                db.Perfis.Remove(perfil);
                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            }
        ).RequireAuthorization();

        route.MapPatch("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                PerfilInputPatchDto input, 
                CrudContext db, 
                HttpContext httpContext, 
                IValidator<(Guid id, PerfilInputPatchDto input)>validator) =>
            {     

                var validationResult = await validator.ValidateAsync((id, input));
        
                if (!validationResult.IsValid)
                {
                    // Trata o 404 do Usuário
                    if (validationResult.Errors.Any(e => e.ErrorCode == "NotFound"))
                        return Results.NotFound(new { message = validationResult.Errors.First(e => e.ErrorCode == "NotFound").ErrorMessage });

                    // Trata o 404 do Perfil
                    if (validationResult.Errors.Any(e => e.ErrorCode == "PerfilNotFound"))
                        return Results.NotFound(new { message = validationResult.Errors.First(e => e.ErrorCode == "PerfilNotFound").ErrorMessage });

                    // Retorna 400 para erros cadastrais normais (E-mail duplicado, formato de senha, etc)
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = validationResult.ToDictionary() });
                }

                var perfil = await db.Perfis.FindAsync(id);            

                if (!string.IsNullOrEmpty(input.Nome)) perfil.Nome = input.Nome; 
                if (!string.IsNullOrEmpty(input.Descricao)) perfil.Descricao = input.Descricao;
                if (!string.IsNullOrEmpty(input.Ativo)) perfil.Ativo = input.Ativo.ToUpper();
                
                await db.SaveChangesAsync();

                List<HyperLink>links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);

                return TypedResults.Ok(new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
            }
        ).RequireAuthorization();        
    }
    
}
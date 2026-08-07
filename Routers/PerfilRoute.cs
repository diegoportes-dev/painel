using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Crud.Data;
using Microsoft.EntityFrameworkCore;

public sealed record PerfilCollectionResponse(IReadOnlyList<PerfilOutputDto> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);
public sealed record PerfilResourceResponse(PerfilOutputDto Data, IReadOnlyList<HyperLink> Links);

public static class PerfilRoute
{    
    public static void MapPerfilRoutes(this WebApplication app)
    {
        string prefixo = "perfis";
        var route = app.MapGroup($"/{prefixo}");

        route.MapGet("", 
            async Task<IResult> (int? page, int? pageSize, CrudContext db, HttpContext httpContext) =>
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
                    new("self", $"{baseUrl}/perfis{pageQuery}", "GET"),
                    new("collection", $"{baseUrl}/perfis", "GET"),
                    new("create", $"{baseUrl}/perfis", "POST")
                };

                if (pageNumber > 1)
                {
                    links.Add(new HyperLink("prev", $"{baseUrl}/perfis?page={pageNumber - 1}&pageSize={requestedPageSize}", "GET"));
                }

                if (pageNumber < totalPages)
                {
                    links.Add(new HyperLink("next", $"{baseUrl}/perfis?page={pageNumber + 1}&pageSize={requestedPageSize}", "GET"));
                }

                if (totalPages > 0)
                {
                    links.Add(new HyperLink("first", $"{baseUrl}/perfis?page=1&pageSize={requestedPageSize}", "GET"));
                    links.Add(new HyperLink("last", $"{baseUrl}/perfis?page={totalPages}&pageSize={requestedPageSize}", "GET"));
                }

                return TypedResults.Ok(new PerfilCollectionResponse(
                    perfis,
                    new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                    links));
            }
        );

        route.MapPost("", 
            async Task<IResult> (PerfilInputPostDTO input, CrudContext db, HttpContext httpContext) =>
            {   
                try
                {

                        var errosValidacao = ValidateDataAnnotations.Validate(input);
                        if (errosValidacao != null)
                        {
                            return errosValidacao; // Retorna HTTP 400 Bad Request com a lista de erros estruturada
                        }

                        var perfil = new PerfilModel(input);

                        db.Perfis.Add(perfil);
                        await db.SaveChangesAsync();

                        List<HyperLink> links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);

                        return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{perfil.Id}", new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
                
                } 
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                {                       
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;

                    return TypedResults.BadRequest(new 
                    { 
                        message = "Erro ao criar o perfil. Verifique os dados enviados.",
                        technicalDetails = detalheTecnico
                    }); 
                } 
                catch (Exception ex)
                {
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;
            
                    return TypedResults.Problem(
                        title: "Ocorreu um erro interno inesperado ao criar perfil.",
                        detail: detalheTecnico, 
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }               
            }
        );

        route.MapGet("/{id:guid}", 
            async Task<IResult> (Guid id, CrudContext db, HttpContext httpContext) =>
            {
                var perfil = await db.Perfis.FindAsync(id);
                if (perfil is null)
                {
                    return TypedResults.NotFound();
                }

                List<HyperLink> links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);
                
                return TypedResults.Ok(new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
            }
        );

        route.MapPut("/{id:guid}", 
            async Task<IResult> (Guid id, PerfilInputPutDTO input, CrudContext db, HttpContext httpContext) =>
            {
                try
                {

                        var errosValidacao = ValidateDataAnnotations.Validate(input);
                        if (errosValidacao != null)
                        {
                            return errosValidacao; // Retorna HTTP 400 Bad Request com a lista de erros estruturada
                        }

                        var perfil = await db.Perfis.FindAsync(id);
                        if (perfil is null)
                        {
                            return TypedResults.NotFound(new { message = $"Perfil com ID {id} não encontrado." });
                        }

                        perfil.UpdatePerfil(input);

                        await db.SaveChangesAsync();

                        List<HyperLink> links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);

                        return TypedResults.Ok(new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
                    
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                {                       
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;

                    return TypedResults.BadRequest(new 
                    { 
                        message = "Erro ao criar o perfil. Verifique os dados enviados.",
                        technicalDetails = detalheTecnico
                    }); 
                } 
                catch (Exception ex)
                {
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;
            
                    return TypedResults.Problem(
                        title: "Ocorreu um erro interno inesperado ao atualizar perfil.",
                        detail: detalheTecnico, 
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }
            }
        );

        route.MapDelete("/{id:guid}", 
            async Task<IResult> (Guid id, CrudContext db) =>
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
        );

        route.MapPatch("/{id:guid}", 
            async Task<IResult> (Guid id, PerfilInputPutDTO input, CrudContext db, HttpContext httpContext) =>
            {                
                var perfil = await db.Perfis.FindAsync(id);
                if (perfil is null)
                {
                    return TypedResults.NotFound(new { message = $"Perfil com ID {id} não encontrado." });
                }

                if (!string.IsNullOrEmpty(input.Nome))
                {
                    perfil.Nome = input.Nome;
                }

                if (!string.IsNullOrEmpty(input.Descricao))
                {
                    perfil.Descricao = input.Descricao;
                }

                if (!string.IsNullOrEmpty(input.Ativo))
                {
                    perfil.Ativo = input.Ativo;
                }

                var validationResult = ValidateDataAnnotations.Validate( new PerfilInputPutDTO(perfil) );
                if (validationResult is not null)
                {
                    return validationResult;
                }

                await db.SaveChangesAsync();

                List<HyperLink>links = Links.GenerateLinks(httpContext, perfil.Id, prefixo);

                return TypedResults.Ok(new PerfilResourceResponse(new PerfilOutputDto(perfil), links));
            }
        );        
    }
    
}
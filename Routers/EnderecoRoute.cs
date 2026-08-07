// namespace Crud;

using System.ComponentModel.DataAnnotations;
using Crud.Data;
using Microsoft.EntityFrameworkCore;

// public sealed record HyperLink(string Rel, string Href, string Method);
// public sealed record PaginationMetadata(int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record EnderecoCollectionResponse(IReadOnlyList<EnderecoOutputDto> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);
public sealed record EnderecoResourceResponse(EnderecoOutputDto Data, IReadOnlyList<HyperLink> Links);

public static class EnderecoRoute
{
    private static IResult? ValidateInput(object request)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);

        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults
                .SelectMany(result =>
                {
                    var members = result.MemberNames.Any() ? result.MemberNames : new[] { string.Empty };
                    return members.Select(member => new { Member = member, Error = result.ErrorMessage ?? "Valor inválido." });
                })
                .GroupBy(item => item.Member)
                .ToDictionary(group => group.Key, group => group.Select(item => item.Error).Distinct().ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        return null;
    }

    public static void MapEnderecoRoutes(this WebApplication app)
    {
        var route = app.MapGroup("/enderecos");
  
        // route.MapGet("", 
        //     async Task<IResult> (CrudContext db) =>
        //     {
        //         var enderecos = await db.Endereco.Select(res => new EnderecoDto(res)).ToArrayAsync();

        //         return enderecos is null    
        //             ? TypedResults.NotFound()
        //             : TypedResults.Ok(enderecos);
        //     }
        // );

        route.MapGet("",
            async Task<IResult> (int? page, int? pageSize, CrudContext db, HttpContext httpContext) =>
            {
                var pageNumber = page is null or < 1 ? 1 : page.Value;
                var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;

                var totalItems = await db.Endereco.CountAsync();
                var totalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

                var enderecos = await db.Endereco
                    .Include(e => e.Person)
                    .OrderBy(e => e.Endereco)
                    .Skip((pageNumber - 1) * requestedPageSize)
                    .Take(requestedPageSize)
                    .Select(res => new EnderecoOutputDto(res))
                    .ToListAsync();

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var pageQuery = $"?page={pageNumber}&pageSize={requestedPageSize}";

                var links = new List<HyperLink>
                {
                    new("self", $"{baseUrl}/enderecos{pageQuery}", "GET"),
                    new("collection", $"{baseUrl}/enderecos", "GET"),
                    new("create", $"{baseUrl}/enderecos", "POST")
                };

                if (pageNumber > 1)
                {
                    links.Add(new HyperLink("prev", $"{baseUrl}/enderecos?page={pageNumber - 1}&pageSize={requestedPageSize}", "GET"));
                }

                if (pageNumber < totalPages)
                {
                    links.Add(new HyperLink("next", $"{baseUrl}/enderecos?page={pageNumber + 1}&pageSize={requestedPageSize}", "GET"));
                }

                if (totalPages > 0)
                {
                    links.Add(new HyperLink("first", $"{baseUrl}/enderecos?page=1&pageSize={requestedPageSize}", "GET"));
                    links.Add(new HyperLink("last", $"{baseUrl}/enderecos?page={totalPages}&pageSize={requestedPageSize}", "GET"));
                }

                return TypedResults.Ok(new EnderecoCollectionResponse(
                    enderecos,
                    new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                    links));
            }
        ).RequireAuthorization();


        route.MapPost("",
            async Task<IResult> (EnderecoInputDto req, CrudContext db, HttpContext httpContext ) =>
            {
                var validationProblem = ValidateInput(req);
                if (validationProblem is not null)
                {
                    return validationProblem;
                }

                var personId = Guid.Parse(req.PersonId!);

                var person = await db.People
                    .FirstOrDefaultAsync(p => p.Id == personId);

                if (person is null)
                {
                    return Results.NotFound(new { message = $"Pessoa com ID {personId} não encontrada." });
                }

                var endereco = new EnderecoModel {
                    Endereco = req.Endereco,
                    Numero = req.Numero,
                    Bairro = req.Bairro,
                    Cidade = req.Cidade,
                    Uf = req.Uf,
                    Cep = req.Cep,
                    PersonId = personId,
                    Person = person  
                };

                await db.Endereco.AddAsync(endereco);
                await db.SaveChangesAsync();

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var links = new List<HyperLink>
                {
                    new("self", $"{baseUrl}/enderecos/{endereco.Id}", "GET"),
                    new("collection", $"{baseUrl}/enderecos", "GET"),
                    new("update", $"{baseUrl}/enderecos/{endereco.Id}", "PUT"),
                    new("delete", $"{baseUrl}/enderecos/{endereco.Id}", "DELETE")
                };

                // var retorno = new EnderecoOutputDto(endereco);                
                // return Results.Created($"/enderecos/{retorno.Id}", retorno);
                
                return TypedResults.Created($"{baseUrl}/enderecos/{endereco.Id}", new EnderecoResourceResponse(new EnderecoOutputDto(endereco), links));
            }
        );

        // route.MapGet("/{id}", 
        //     async Task<IResult>(Guid id, CrudContext db) =>
        //     {
        //         var endereco = await db.Endereco.FindAsync(id);

        //         return endereco is null
        //             ? TypedResults.NotFound()
        //             : TypedResults.Ok( new EnderecoDto(endereco));

        //     }
        // );

        route.MapGet("/{id}",
            async Task<IResult>(Guid id, CrudContext db, HttpContext httpContext) =>
            {
                var endereco = await db.Endereco
                    .Include(e => e.Person)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (endereco is null)
                {
                    return TypedResults.NotFound();
                }

                var dto = new EnderecoOutputDto(endereco);
                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var links = new List<HyperLink>
                {
                    new("self", $"{baseUrl}/enderecos/{id}", "GET"),
                    new("collection", $"{baseUrl}/enderecos", "GET"),
                    new("update", $"{baseUrl}/enderecos/{id}", "PUT"),
                    new("patch", $"{baseUrl}/enderecos/{id}", "PATCH"),
                    new("delete", $"{baseUrl}/enderecos/{id}", "DELETE")
                };

                return TypedResults.Ok(new EnderecoResourceResponse(dto, links));
            }
        );

        

        route.MapPut("/{id}", 
            async Task<IResult>(Guid id, EnderecoInputDto req, CrudContext db) =>
            {
                var validationProblem = ValidateInput(req);
                if (validationProblem is not null)
                {
                    return validationProblem;
                }

                var personId = Guid.Parse(req.PersonId!);

                var person = await db.People
                    .FirstOrDefaultAsync(p => p.Id == personId);

                if (person is null)
                {
                    return Results.NotFound(new { message = $"Pessoa com ID {personId} não encontrada." });
                }


                var endereco = await db.Endereco.FindAsync(id);

                if (endereco is null) return TypedResults.NotFound();

                endereco.Endereco = req.Endereco;
                endereco.Numero = req.Numero;
                endereco.Bairro = req.Bairro;
                endereco.Cidade = req.Cidade;
                endereco.Uf = req.Uf;
                endereco.Cep = req.Cep;
                endereco.PersonId = personId;

                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            }
        );

        route.MapDelete("/{id}", 
            async Task<IResult>(Guid id, CrudContext db) =>
            {
                var endereco = await db.Endereco.FindAsync(id);

                if (endereco is null) return TypedResults.NotFound();

                db.Endereco.Remove(endereco);
                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            }
        );

        route.MapPatch("/{id}", 
            async Task<IResult>(Guid id, EnderecoPatchDto req, CrudContext db) =>
            {
                var validationProblem = ValidateInput(req);
                if (validationProblem is not null)
                {
                    return validationProblem;
                }

                var endereco = await db.Endereco.FindAsync(id);

                if (endereco is null) return TypedResults.NotFound();

                var personId = endereco.PersonId;
                
                if (!string.IsNullOrWhiteSpace(req.PersonId))
                {
                    personId = Guid.Parse(req.PersonId);

                    var person = await db.People
                        .FirstOrDefaultAsync(p => p.Id == personId);
                    
                    if (person is null)
                    {
                        return Results.NotFound(new { message = $"Pessoa com ID {personId} não encontrada." });
                    }
                }

                endereco.Endereco = req.Endereco ?? endereco.Endereco;
                endereco.Numero = req.Numero ?? endereco.Numero;
                endereco.Bairro = req.Bairro ?? endereco.Bairro;
                endereco.Cidade = req.Cidade ?? endereco.Cidade;
                endereco.Uf = req.Uf ?? endereco.Uf;
                endereco.Cep = req.Cep ?? endereco.Cep;
                endereco.PersonId = personId;

                await db.SaveChangesAsync();

                return TypedResults.NoContent();
            }
        );

    }
}
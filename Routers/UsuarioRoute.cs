using Crud.Data;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;
using FluentValidation;

public sealed record UsuarioCollectionResponse(IReadOnlyList<UsuarioOutputDto> Data, PaginationMetadata Pagination, IReadOnlyList<HyperLink> Links);
public sealed record UsuarioResourceResponse(UsuarioOutputDto Data, IReadOnlyList<HyperLink> Links);

public static class UsuarioRoute
{
    public static object BCryptNet { get; private set; }

    public static void MapUsuarioRoutes(this WebApplication app)
    {
        string prefixo = "usuarios";
        var route = app.MapGroup($"/{prefixo}");

        route.MapGet("", 
            async Task<IResult> (int? page, int? pageSize, CrudContext db, HttpContext httpContext) =>
            {
                var pageNumber = page is null or < 1 ? 1 : page.Value;
                var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;

                var totalItems = await db.Usuarios.CountAsync();
                var totalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

                var usuarios = await db.Usuarios
                    .Include(u => u.Perfil)
                    .OrderBy(u => u.Email)
                    .Skip((pageNumber - 1) * requestedPageSize)
                    .Take(requestedPageSize)
                    .Select(u => new UsuarioOutputDto(u))
                    .ToListAsync();

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var pageQuery = $"?page={pageNumber}&pageSize={requestedPageSize}";

                var links = new List<HyperLink>
                {
                    new HyperLink("self", $"{baseUrl}/{prefixo}{pageQuery}", "GET"),
                    new HyperLink("first", $"{baseUrl}/{prefixo}?page=1&pageSize={requestedPageSize}", "GET"),
                    new HyperLink("last", $"{baseUrl}/{prefixo}?page={totalPages}&pageSize={requestedPageSize}", "GET"),
                    new HyperLink("next", pageNumber < totalPages ? $"{baseUrl}/{prefixo}?page={pageNumber + 1}&pageSize={requestedPageSize}" : null, "GET"),
                    new HyperLink("prev", pageNumber > 1 ? $"{baseUrl}/{prefixo}?page={pageNumber - 1}&pageSize={requestedPageSize}" : null, "GET")
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

                return TypedResults.Ok(new UsuarioCollectionResponse(
                    usuarios,
                    new PaginationMetadata(pageNumber, requestedPageSize, totalItems, totalPages),
                    links));
            }
        );

        route.MapPost("",
            async Task<IResult> (UsuarioInputPostDto input, CrudContext db, HttpContext httpContext, FluentValidation.IValidator<UsuarioInputPostDto> validator) =>
            {
                
                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    // Formata os erros em um dicionário amigável (Propriedade -> Mensagens de erro)
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

                input.Senha = BCrypt.Net.BCrypt.HashPassword(input.Senha);

                var usuario = new UsuarioModel(input);

                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                var usuarioCommit = await db.Usuarios
                    .Include(u => u.Perfil)
                    .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{usuario.Id}", new UsuarioResourceResponse(new UsuarioOutputDto(usuarioCommit), links));

            }
        );

        route.MapGet("/{id:guid}", 
            async Task<IResult> (Guid id, CrudContext db, HttpContext httpContext) =>
            {
                var usuario = await db.Usuarios
                    .Include(u => u.Perfil)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario is null)
                {
                    return TypedResults.NotFound();
                }

                List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                return TypedResults.Ok(new UsuarioResourceResponse(new UsuarioOutputDto(usuario), links));
            }
        ); 

        route.MapPut("/{id:guid}",
            async Task<IResult> (Guid id, UsuarioInputPutDto input, CrudContext db, HttpContext httpContext) =>
            {
                try
                {            

                        var validationResult = ValidateDataAnnotations.Validate(input);
                        if (validationResult is not null)
                        {
                            return validationResult;
                        }

                        var usuario = await db.Usuarios
                            .FirstOrDefaultAsync(u => u.Id == id);

                        if (usuario is null)
                        {
                            return Results.NotFound(new { message = $"Usuário com ID {id} não encontrado." });
                        }

                        if(!string.IsNullOrEmpty(input.NovaSenha) && !BCrypt.Net.BCrypt.Verify(input.NovaSenha, usuario.SenhaCrypt))
                        {
                            input.NovaSenha = BCrypt.Net.BCrypt.HashPassword(input.NovaSenha);
                        }
                        else
                        {
                            input.NovaSenha = usuario.SenhaCrypt;
                        }

                        var emailExist = await db.Usuarios
                            .FirstOrDefaultAsync(u => u.Email == input.Email && u.Id != id);

                        if (emailExist is not null)
                        {
                            return Results.BadRequest(new { message = $"E-Mail {input.Email} já está em uso por outro usuário." });
                        }

                        var perfilId = Guid.Parse(input.PerfilId.ToString());

                        var perfil = await db.Perfis
                            .FirstOrDefaultAsync(p => p.Id == perfilId);

                        if (perfil is null)
                        {
                            return Results.NotFound(new { message = $"Perfil com ID {perfilId} não encontrado." });
                        }

                        usuario.UpdateUsuario(input);

                        await db.SaveChangesAsync();

                        List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                        return TypedResults.Ok(new UsuarioResourceResponse(new UsuarioOutputDto(usuario), links));

                    
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                {                       
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;

                    return TypedResults.BadRequest(new 
                    { 
                        message = "Erro ao atualizar o usuário. Verifique os dados enviados.",
                        technicalDetails = detalheTecnico
                    }); 
                } 
                catch (Exception ex)
                {
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;
            
                    return TypedResults.Problem(
                        title: "Ocorreu um erro interno inesperado ao atualizar usuario.",
                        detail: detalheTecnico, 
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                } 
            }   
        ); 

        route.MapDelete("/{id:guid}",
            async Task<IResult> (Guid id, CrudContext db) =>
            {
                var usuario = await db.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario is null)
                {
                    return Results.NotFound(new { message = $"Usuário com ID {id} não encontrado." });
                }

                db.Usuarios.Remove(usuario);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }
        );

        route.MapPatch("/{id:guid}",
            async Task<IResult> (Guid id, UsuarioInputPutDto input, CrudContext db, HttpContext httpContext) =>
            {    
                try
                {
                        var senhaAntiga = string.Empty;

                        var usuario = await db.Usuarios
                            .FirstOrDefaultAsync(u => u.Id == id);

                        if (usuario is null)
                        {
                            return Results.NotFound(new { message = $"Usuário com ID {id} não encontrado." });
                        }

                        if (!string.IsNullOrEmpty(input.Email) && input.Email != usuario.Email)
                        {
                            var emailExist = await db.Usuarios
                            .FirstOrDefaultAsync(p => p.Email == input.Email);

                            if (emailExist is not null)
                            {
                                return Results.BadRequest(new { message = $"E-Mail {input.Email} já está em uso por outro usuário." });
                            }

                            usuario.Email = input.Email;
                        }

                        if (!string.IsNullOrEmpty(input.NovaSenha) && !BCrypt.Net.BCrypt.Verify(input.NovaSenha, usuario.SenhaCrypt))
                        {
                            usuario.SenhaCrypt = input.NovaSenha; //para que seja validada pelo DattaAnotation não Encrypto
                        }else{
                            senhaAntiga = usuario.SenhaCrypt;
                            usuario.SenhaCrypt = "Teste123"; //Só para passar no DataValidation, depois será substituida pela senha antiga.
                        }

                        if (!string.IsNullOrEmpty(input.Ativo))
                        {
                            usuario.Ativo = input.Ativo;
                        }

                        if (input.PerfilId != Guid.Empty)
                        {
                            var perfilId = Guid.Parse(input.PerfilId.ToString());

                            var perfil = await db.Perfis
                                .FirstOrDefaultAsync(p => p.Id == perfilId);

                            if (perfil is null)
                            {
                                return Results.NotFound(new { message = $"Perfil com ID {perfilId} não encontrado." });
                            }

                            usuario.PerfilId = perfilId;
                        }

                        if (!string.IsNullOrEmpty(input.Ativo))
                        {
                            usuario.Ativo = input.Ativo;
                        }

                        //Aqui forço as validações DataAnnotations do UsuarioInputDto
                        var validationResult = ValidateDataAnnotations.Validate( new UsuarioInputPutDto(usuario) ); 
                        if (validationResult is not null)
                        {
                            return validationResult;
                        }

                        if (!string.IsNullOrEmpty(input.NovaSenha) && input.NovaSenha == usuario.SenhaCrypt)
                        {
                            usuario.SenhaCrypt =  BCrypt.Net.BCrypt.HashPassword(input.NovaSenha); //Cryptografo nesse ponto após validação.
                        }else{                            
                            usuario.SenhaCrypt = senhaAntiga; //Retorno a senha antiga caso não tenha sido alterada.
                        }

                        await db.SaveChangesAsync();
                    
                        List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                        var response = await db.Usuarios
                            .Include(u => u.Perfil)
                            .FirstOrDefaultAsync(u => u.Id == id);

                        return TypedResults.Ok(new UsuarioResourceResponse(new UsuarioOutputDto(response), links));
                    
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                {                       
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;

                    return TypedResults.BadRequest(new 
                    { 
                        message = "Erro ao atualizar o usuário. Verifique os dados enviados.",
                        technicalDetails = detalheTecnico
                    }); 
                } 
                catch (Exception ex)
                {
                    var detalheTecnico = ex.InnerException?.Message ?? ex.Message;
            
                    return TypedResults.Problem(
                        title: "Ocorreu um erro interno inesperado ao atualizar usuário.",
                        detail: detalheTecnico, 
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                }             
            }
        );
        
    }
}

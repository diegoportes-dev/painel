using Crud.Data;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;
using FluentValidation;
using System.Security.Claims;

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
            async Task<IResult> (
                int? page, 
                int? pageSize, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var pageNumber = page is null or < 1 ? 1 : page.Value;
                var requestedPageSize = pageSize is null or < 1 ? 10 : pageSize.Value;

                // 1. Extrai o ID do Usuário operador logado da Claim do JWT
                var logadoIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(logadoIdClaim) || !Guid.TryParse(logadoIdClaim, out var logadoUserId))
                {
                    return Results.Json(new { message = "Usuário operador não identificado ou token inválido." }, statusCode: 401);
                }

                // 2. Consulta rápida na base para descobrir o TenantId do operador atual
                var operadorTenantId = await db.Usuarios
                    .Where(u => u.Id == logadoUserId)
                    .Select(u => new {u.TenantId, u.Master} )
                    .FirstOrDefaultAsync();

                // 3. Constrói a query base aplicando o isolamento lógico               
                var queryBase = db.Usuarios.AsQueryable();
                
                if (operadorTenantId.TenantId.HasValue && operadorTenantId.Master == false)
                {
                    queryBase = queryBase.Where(u => u.TenantId == operadorTenantId.TenantId);
                }

                // 4. Executa a contagem total baseada na query filtrada da empresa
                var totalItems = await queryBase.CountAsync();
                var totalPages = totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(totalItems / (double)requestedPageSize);

                // 5. Executa a busca paginada e projetada usando o mesmo filtro de isolamento
                var usuarios = await queryBase
                    .Include(u => u.Perfil)
                    .Include(u => u.Tenant)
                    .OrderBy(u => u.Email)
                    .Skip((pageNumber - 1) * requestedPageSize)
                    .Take(requestedPageSize)
                    .Select(u => new UsuarioOutputDto(u))
                    .ToListAsync();

                var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                var pageQuery = $"?page={pageNumber}&pageSize={requestedPageSize}";

                // 6. Geração limpa e corrigida dos links HATEOAS (Sem duplicidades ou nulos)
                var links = new List<HyperLink>
                {
                    new HyperLink("self", $"{baseUrl}/{prefixo}{pageQuery}", "GET"),
                    new HyperLink("collection", $"{baseUrl}/{prefixo}", "GET")
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
        ).RequireAuthorization("ValidarRequisitosPerfil");


        route.MapPost("",
            async Task<IResult> (
                UsuarioInputPostDto input, 
                CrudContext db, 
                HttpContext httpContext, 
                FluentValidation.IValidator<UsuarioInputPostDto> validator) =>
            {
                
                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    // Formata os erros em um dicionário amigável (Propriedade -> Mensagens de erro)
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

                 // 2. Extrai o ID do Usuário Admin que está logado (NameIdentifier do JWT)
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    return Results.Json(new { message = "Usuário operador não identificado ou token inválido." }, statusCode: 401);
                }

                // 3. Consulta direta na base central para descobrir o TenantId do Admin operador
                var adminTenantId = await db.Usuarios
                    .Where(u => u.Id == userId)
                    .Select(u => u.TenantId)
                    .FirstOrDefaultAsync();

                // Valida se o Admin realmente pertence a um Tenant ativo
                if (adminTenantId == null || adminTenantId == Guid.Empty)
                {
                    return Results.Json(new { message = "O usuário operador não possui um Tenant associado." }, statusCode: 403);
                }

                input.Senha = BCrypt.Net.BCrypt.HashPassword(input.Senha);

                var usuario = new UsuarioModel(input, adminTenantId);

                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                var usuarioCommit = await db.Usuarios
                    .Include(u => u.Perfil)
                    .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                return TypedResults.Created($"{Links.BaseUrl(httpContext)}/{prefixo}/{usuario.Id}", new UsuarioResourceResponse(new UsuarioOutputDto(usuarioCommit), links));

            }
        ).RequireAuthorization();

        route.MapGet("/{id:guid}", 
            async Task<IResult> (
                Guid id, 
                CrudContext db, 
                HttpContext httpContext) =>
            {
                var usuario = await db.Usuarios
                    .Include(u => u.Perfil)
                    .Include(u => u.Tenant)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario is null)
                {
                    return TypedResults.NotFound();
                }

                List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                return TypedResults.Ok(new UsuarioResourceResponse(new UsuarioOutputDto(usuario), links));
            }
        ).RequireAuthorization(); 

        route.MapPut("/{id:guid}",
            async Task<IResult>(
                Guid id, 
                UsuarioInputPutDto input, 
                CrudContext db, 
                HttpContext httpContext, 
                FluentValidation.IValidator<(Guid Id, UsuarioInputPutDto Input)> validator) =>
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

                var usuario = await db.Usuarios.FirstAsync(u => u.Id == id);

                if (!string.IsNullOrEmpty(input.NovaSenha) && !BCrypt.Net.BCrypt.Verify(input.NovaSenha, usuario.SenhaCrypt))
                {
                    input.NovaSenha = BCrypt.Net.BCrypt.HashPassword(input.NovaSenha);
                }
                else
                {
                    input.NovaSenha = usuario.SenhaCrypt;
                }

                // db.Usuarios.Update(usuario);

                usuario.UpdateUsuario(input);

                await db.SaveChangesAsync();                

                List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                var usuarioCommit = await db.Usuarios
                    .Include(u => u.Perfil)
                    .Include(u => u.Tenant)
                    .FirstOrDefaultAsync(u => u.Id == usuario.Id);

                return TypedResults.Ok(new UsuarioResourceResponse(new UsuarioOutputDto(usuarioCommit), links));
                 
            }   
        ).RequireAuthorization(); 

        route.MapDelete("/{id:guid}",
            async Task<IResult> (
                Guid id, 
                CrudContext db) =>
            {
                var usuario = await db.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario is null)
                {
                    return Results.NotFound(new { message = $"Usuário não encontrado." });
                }

                db.Usuarios.Remove(usuario);
                await db.SaveChangesAsync();

                return Results.NoContent();
            }
        ).RequireAuthorization();

        route.MapPatch("/{id:guid}",
            async Task<IResult> (
                Guid id, 
                UsuarioInputPatchDto input, 
                CrudContext db, 
                HttpContext httpContext,
                IValidator<(Guid id, UsuarioInputPatchDto input)> validator) =>
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
            
                var usuario = await db.Usuarios.FirstAsync(u => u.Id == id);
                
                if (!string.IsNullOrEmpty(input.Email)) usuario.Email = input.Email; 
                if (!string.IsNullOrEmpty(input.Ativo)) usuario.Ativo = input.Ativo;
                if (input.PerfilId != Guid.Empty) usuario.PerfilId = input.PerfilId;

                if (!string.IsNullOrEmpty(input.NovaSenha))
                {
                    if (!BCrypt.Net.BCrypt.Verify(input.NovaSenha, usuario.SenhaCrypt))
                    {
                        usuario.SenhaCrypt = BCrypt.Net.BCrypt.HashPassword(input.NovaSenha);
                    }
                }
                
                await db.SaveChangesAsync();

                List<HyperLink> links = Links.GenerateLinks(httpContext, usuario.Id, prefixo);

                var usuarioCommit = await db.Usuarios
                    .Include(u => u.Perfil)
                    .Include(u => u.Tenant)
                    .FirstOrDefaultAsync(u => u.Id == id);

                return TypedResults.Ok(new UsuarioResourceResponse(new UsuarioOutputDto(usuarioCommit), links));
            }
    ).RequireAuthorization();

        
    }
}

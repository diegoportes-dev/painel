using Crud.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCryptNet = BCrypt.Net.BCrypt;

public static class LoginRoute
{    
    public static void MapLoginRoutes(this WebApplication app, byte[] chaveEmBytes)
    {
        var route = app.MapGroup("/login");

        route.MapPost("", async Task<IResult> (LoginInputDto input, CrudContext db) =>
        {
            var errosValidacao = ValidateDataAnnotations.Validate(input);
            if (errosValidacao != null)
            {
                return errosValidacao; // Retorna HTTP 400 Bad Request com a lista de erros estruturada
            }

            var usuario = await db.Usuarios
            .Include(u => u.Perfil)
            .FirstOrDefaultAsync(u => u.Email == input.Email);
    
            if (usuario == null || !BCryptNet.Verify(input.Senha, usuario.SenhaCrypt))
            {
                return Results.Unauthorized();
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())]),
                // Expires = DateTime.UtcNow.AddHours(2), //Duas horas de validade
                Expires = DateTime.UtcNow.AddSeconds(30),  //Trinta segundos de validade para teste 
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(chaveEmBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new { token = tokenString });
        });

        // 2. [POST] /login/esqueci-senha (Gera o Token de Recuperação)
        route.MapPost("/esqueci-senha", async Task<IResult> (EsqueciSenhaInputDto input, CrudContext db) =>
        {
            var errosValidacao = ValidateDataAnnotations.Validate(input);
            if (errosValidacao != null) return errosValidacao;

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == input.Email);
            
            // Segurança contra enumeração de e-mails: Retorna OK mesmo se o e-mail não existir.
            if (usuario == null)
            {
                return Results.Ok(new { message = "Se o e-mail existir no sistema, um token de recuperação foi enviado." });
            }

            // Gera um token aleatório simples de 6 dígitos numéricos (ou um Guid)
            string tokenReset = Random.Shared.Next(100000, 999999).ToString();
            
            // Define o token e o tempo de expiração (15 minutos a partir de agora)
            usuario.TokenReset = tokenReset; // Adicione esse campo na sua Model de Usuário
            // usuario.TokenResetExpiracao = DateTime.UtcNow.AddMinutes(1); // Adicione esse campo na sua Model
            usuario.TokenResetExpiracao = DateTime.Now.AddMinutes(1); // Adicione esse campo na sua Model

            await db.SaveChangesAsync();

            // TODO: Aqui você integraria seu serviço de e-mail (ex: SendGrid, SMTP)
            // Por enquanto, exibimos no console para testes locais:
            Console.WriteLine($"[EMAIL SIMULADO] Token de reset para {usuario.Email}: {tokenReset}");

            return Results.Ok(new { message = "Se o e-mail existir no sistema, um token de recuperação foi enviado." });
        });

        // 3. [POST] /login/resetar-senha (Valida o token e altera para a nova senha com BCrypt)
        route.MapPost("/resetar-senha", async Task<IResult> (ResetarSenhaInputDto input, CrudContext db) =>
        {
            // Valida as regras de DataAnnotation da nova senha automaticamente
            var errosValidacao = ValidateDataAnnotations.Validate(input);
            if (errosValidacao != null) return errosValidacao;

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == input.Email);

            DateTime? dataExpiracaoLocal = usuario?.TokenResetExpiracao.HasValue == true
            ? DateTime.SpecifyKind(usuario.TokenResetExpiracao.Value, DateTimeKind.Local)
            : null;

            if (usuario == null || 
                usuario.TokenReset != input.Token || 
                // usuario.TokenResetExpiracao < DateTime.UtcNow)
                usuario.TokenResetExpiracao < DateTime.Now)
            {
                return Results.BadRequest(new { message = "Token inválido, expirado ou e-mail incorreto." });
            }

            // Aplica o Hash seguro do BCrypt na NOVA senha validada
            usuario.SenhaCrypt = BCryptNet.HashPassword(input.NovaSenha);

            // Invalida o token para não ser reutilizado
            usuario.TokenReset = null;
            usuario.TokenResetExpiracao = null;

            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Senha redefinida com sucesso!" });
        });
    }
}


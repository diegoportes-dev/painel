using Crud.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCryptNet = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Options;
using FluentValidation;

public static class LoginRoute
{
    public static void MapLoginRoutes(this WebApplication app, byte[] chaveEmBytes)
    {
        var route = app.MapGroup("/login");

        route.MapPost("", 
            async Task<IResult> (
                LoginInputDto input, 
                CrudContext db, 
                IOptions<TimeOutSettings> timeoutOptions,
                IValidator<LoginInputDto>validator ) =>
            {
                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    // Formata os erros em um dicionário amigável (Propriedade -> Mensagens de erro)
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

                var usuario = await db.Usuarios
                .Include(u => u.Perfil)
                .FirstOrDefaultAsync(u => u.Email == input.Email && u.Ativo.ToUpper() == "S" );
        
                if (usuario == null || !BCryptNet.Verify(input.Senha, usuario.SenhaCrypt))
                {
                    return Results.Unauthorized();
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                        new Claim(ClaimTypes.Name , usuario.Email),
                        new Claim("TenantId", usuario.TenantId.ToString()!),
                        // new Claim(ClaimTypes.Role, usuario.Perfil?.Nome ?? "Usuario"),
                        // Usando parâmetros customizados (Criados por você)
                        new Claim("PerfilId", usuario.PerfilId.ToString()),
                    ]),
                    // Expires = DateTime.UtcNow.AddHours(2), //Duas horas de validade
                    Expires = DateTime.UtcNow.AddMinutes( timeoutOptions.Value.TokenResetSenha ),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(chaveEmBytes), SecurityAlgorithms.HmacSha256Signature)
                };
                
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Results.Ok(new { token = tokenString });
            });

        // 2. [POST] /login/esqueci-senha (Gera o Token de Recuperação)
        route.MapPost("/esqueci-senha", 
            async Task<IResult> (
                EsqueciSenhaInputDto input, 
                CrudContext db,  
                IEmailService emailService, 
                IOptions<TimeOutSettings> timeoutOptions,
                IValidator<EsqueciSenhaInputDto>validator) =>
            {
                
                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    // Formata os erros em um dicionário amigável (Propriedade -> Mensagens de erro)
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

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
                usuario.TokenResetExpiracao = DateTime.Now.AddMinutes( timeoutOptions.Value.TokenResetExpiracao );

                await db.SaveChangesAsync();

                // Cria o corpo do e-mail formatado em HTML
                string assunto = "Recuperação de Senha";
                string mensagemHtml = $@"
                    <h2>Olá,</h2>
                    <p>Você solicitou a redefinição de sua senha.</p>
                    <p>Seu token de recuperação é válido por { timeoutOptions.Value.TokenResetExpiracao } minutos:</p>
                    <h3 style='color: #007bff; font-size: 24px;'>{tokenReset}</h3>
                    <p>Se você não solicitou este e-mail, ignore-o.</p>";

                // Dispara o e-mail de forma assíncrona em segundo plano
                await emailService.EnviarEmailAsync(usuario.Email, assunto, mensagemHtml);

                // TODO: Aqui você integraria seu serviço de e-mail (ex: SendGrid, SMTP)
                // Por enquanto, exibimos no console para testes locais:
                Console.WriteLine($"[EMAIL SIMULADO] Token de reset para {usuario.Email}: {tokenReset}");

                return Results.Ok(new { message = "Se o e-mail existir no sistema, um token de recuperação foi enviado." });
            });

        // 3. [POST] /login/resetar-senha (Valida o token e altera para a nova senha com BCrypt)
        route.MapPost("/resetar-senha", 
            async Task<IResult> (
                ResetarSenhaInputDto input, 
                CrudContext db,
                IValidator<ResetarSenhaInputDto>validator) =>
            {

                var validationResult = await validator.ValidateAsync(input);
                if (!validationResult.IsValid)
                {
                    // Formata os erros em um dicionário amigável (Propriedade -> Mensagens de erro)
                    var erros = validationResult.ToDictionary();
                    return Results.BadRequest(new { message = "Erros de validação encontrados.", errors = erros });
                }

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

public class TimeOutSettings
{
    public int TokenResetSenha { get; set; }
    public float TokenResetExpiracao { get; set; }
}
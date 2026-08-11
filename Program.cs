// using Crud;
using Crud.Data;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.OpenApi;
using System.ComponentModel.DataAnnotations;


var builder = WebApplication.CreateBuilder(args);

// 1. CARREGAR A CHAVE DINAMICAMENTE DO APPSETTINGS.JSON
var chaveSecreta = builder.Configuration["JwtSettings:ChaveSecreta"];

// Validação de segurança para garantir que a chave foi preenchida
if (string.IsNullOrEmpty(chaveSecreta) || chaveSecreta.Length < 32)
{
    throw new InvalidOperationException("A chave secreta do JWT não foi configurada corretamente ou é muito curta.");
}

var chaveEmBytes = Encoding.ASCII.GetBytes(chaveSecreta);

// 2. Adicionar os serviços de Autenticação e Autorização
builder.Services.AddAuthentication(options =>
{   
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Defina como true em produção
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(chaveEmBytes),
        ValidateIssuer = false,   // Pode ativar e definir o emissor se desejar
        ValidateAudience = false, // Pode ativar e definir o público alvo se desejar
        ValidateLifetime = true,   // Garante que tokens expirados serão rejeitados
        ClockSkew = TimeSpan.Zero  // Remove o tempo de tolerância padrão de 5 minutos
    };
});


// Mapeia a seção do appsettings para a classe C#
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Adicione isso no seu Program.cs antes do builder.Build()
builder.Services.Configure<TimeOutSettings>(builder.Configuration.GetSection("TimeOutSettings"));

// Registra o serviço de e-mail
builder.Services.AddTransient<IEmailService, MailKitEmailService>();


builder.Services.AddAuthorization();

// 3. Configurar o OpenAPI para o Scalar
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<CrudContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// 4. ATIVAR MIDDLEWARES DE SEGURANÇA (Obrigatório antes das rotas)
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();


// app.MapPost("/login",  async(LoginInputDto model, CrudContext db) =>
// {
//     var usuario = await db.Usuarios
//         .Include(u => u.Perfil)
//         .FirstOrDefaultAsync(u => u.Email == model.Email);
    
//     if (usuario == null || usuario.SenhaHash != model.SenhaHash)
//     {
//         return Results.Unauthorized();
//     }

//     // NOVA VALIDAÇÃO: Bloqueia usuários inativos
//     // if (usuario.Ativo == "N")
//     // {
//     //     return Results.Json(new { erro = "Esta conta está inativa." }, statusCode: 403);
//     // }

//     // Simula uma validação de usuário com sucesso
//     var tokenHandler = new JwtSecurityTokenHandler();
//     var tokenDescriptor = new SecurityTokenDescriptor
//     {
//         Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())]),
//          // Expires = DateTime.UtcNow.AddHours(2), //Duas horas de validade
//         Expires = DateTime.UtcNow.AddSeconds(30),  //Trinta segundos de validade para teste 
//         SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(chaveEmBytes), SecurityAlgorithms.HmacSha256Signature)
//     };
    
//     var token = tokenHandler.CreateToken(tokenDescriptor);
//     var tokenString = tokenHandler.WriteToken(token);

//     return Results.Ok(new { token = tokenString });
// });

// Suas rotas existentes
app.MapLoginRoutes(chaveEmBytes);
app.MapPersonRoutes();
app.MapEnderecoRoutes();
app.MapPerfilRoutes();
app.MapUsuarioRoutes();


app.Run();


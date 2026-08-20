// using Crud;
using Crud.Data;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.OpenApi;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Crud.Routers;


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

//Validadores
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAuthorization();

// 3. Configurar o OpenAPI para o Scalar
builder.Services.AddOpenApi();

//Adiciona serviços de EXCEPTIONS unificado
builder.Services.AddProblemDetails();

// 1. Registra o Provedor de Conexão com ciclo de vida Scoped (por requisição)
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

//2. Contexto de Banco
builder.Services.AddScoped<CrudContext>();

// 3. Registra o Banco Operacional (Dinâmico)
builder.Services.AddDbContext<TenantDbContext>();

// Obrigatório para o validador ler o token
builder.Services.AddHttpContextAccessor(); 

//Definições CORs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Porta padrão do Vite
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        context.Response.ContentType = "application/json";

        // Se for um erro de banco de dados (ex: DbUpdateException)
        if (exception is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Erro ao processar a operação no banco de dados. Verifique os dados enviados.",
                technicalDetails = dbEx.InnerException?.Message ?? dbEx.Message
            });
        }
        // Para qualquer outro erro interno inesperado (Exception)
        else if (exception is not null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Ocorreu um erro interno inesperado no servidor.",
                detail = exception.InnerException?.Message ?? exception.Message
            });
        }
    });
});

// 4. ATIVAR MIDDLEWARES DE SEGURANÇA (Obrigatório antes das rotas)
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Suas rotas existentes
app.MapLoginRoutes(chaveEmBytes);
app.MapPerfilRoutes();
app.MapUsuarioRoutes();
app.MapTenantRoutes();



// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<CrudContext>();
    
//     // ==========================================
//     // AJUSTE: Limpa completamente a base antiga
//     // ==========================================
//     // Apaga o arquivo físico do SQLite se ele já existir
//     db.Database.EnsureDeleted(); 

//     // Cria o banco do zero aplicando a estrutura atualizada
//     db.Database.EnsureCreated(); 

//     // Verifica se a tabela de Perfis já tem dados, se não tiver, insere o primeiro
//     if (!db.Perfis.Any())
//     {        
//         var perfilMaster = new PerfilModel 
//         { 
//             Id = Guid.NewGuid(), 
//             Nome = "Administrador Geral", 
//             Descricao = "Suporte do Sistema",
//             Ativo = "S",
//             Created = DateTime.UtcNow,
//             CreatedBy = "Sistema"
//         };
//         db.Perfis.Add(perfilMaster);

//         // Cria o primeiro usuário com o Token de Cadastro para você testar a sua tela inicial
//         var usuarioMaster = new UsuarioModel
//         {
//             Id = Guid.NewGuid(),
//             Email = "suporte@sistema.com",
//             SenhaCrypt = "nao_definida_ainda",
//             PerfilId = perfilMaster.Id,
//             TokenCadastro = "TOKEN123", // Use este token na sua rota de setup-cadastro!
//             TokenCadastroExpiracao = DateTime.UtcNow.AddDays(7),
//             Ativo = "S", // Ajustado para "S" para manter o padrão do perfil
//             Master = true,
//             Created = DateTime.UtcNow,
//             CreatedBy = "Sistema"
//         };
//         db.Usuarios.Add(usuarioMaster);

//         db.SaveChanges();
//     }
// }


// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<CrudContext>();
    
//     // =========================================================================
//     // AJUSTE: Removido EnsureDeleted() para PRESERVAR todos os dados existentes
//     // =========================================================================
//     db.Database.EnsureCreated(); 

//     // 1. Garante a existência do Perfil Master (Se não houver, insere; se houver, preserva)
//     var perfilMaster = await db.Perfis.FirstOrDefaultAsync(p => p.Nome == "Administrador Geral");
    
//     if (perfilMaster == null)
//     {        
//         perfilMaster = new PerfilModel 
//         { 
//             Id = Guid.NewGuid(), 
//             Nome = "Administrador Geral", 
//             Descricao = "Suporte e Administração Global do Sistema",
//             Ativo = "S",
//             Created = DateTime.UtcNow,
//             CreatedBy = "Sistema"
//         };
//         db.Perfis.Add(perfilMaster);
//         await db.SaveChangesAsync(); // Persiste para obter o ID definitivo
//     }

//     // 2. Garante a existência do Usuário de Suporte Inicial (TOKEN123)
//     var existeUsuarioSuporte = await db.Usuarios.AnyAsync(u => u.Email == "suporte3@sistema.com");
//     if (!existeUsuarioSuporte)
//     {
//         var usuarioMaster = new UsuarioModel
//         {
//             Id = Guid.NewGuid(),
//             Email = "suporte3@sistema.com",
//             SenhaCrypt = "nao_definida_ainda",
//             PerfilId = perfilMaster.Id,
//             TokenCadastro = "TOKEN123", 
//             TokenCadastroExpiracao = DateTime.UtcNow.AddDays(7),
//             Ativo = "S", 
//             Created = DateTime.UtcNow,
//             CreatedBy = "Sistema"
//         };
//         db.Usuarios.Add(usuarioMaster);
//     }

//     // =========================================================================
//     // REGRA DE NEGÓCIO: Se o perfil já existe, inclui o OUTRO usuário Admin Geral
//     // =========================================================================
//     var existeAdminGeral = await db.Usuarios.AnyAsync(u => u.Email == "admingeral3@sistema.com");
//     if (!existeAdminGeral)
//     {
//         var usuarioAdminGeral = new UsuarioModel
//         {
//             Id = Guid.NewGuid(),
//             Email = "admingeral3@sistema.com",
//             // Criptografia robusta para o login direto do operador master do catálogo
//             SenhaCrypt = BCrypt.Net.BCrypt.HashPassword("AdminGeral123"), 
//             PerfilId = perfilMaster.Id,
//             TenantId = null, // Opera globalmente na base central (sem isolamento de tenant)
//             TokenCadastro = "TOKEN123", // Use este token na sua rota de setup-cadastro!
//             TokenCadastroExpiracao = DateTime.UtcNow.AddDays(7),           
//             Ativo = "S",
//             Created = DateTime.UtcNow,
//             CreatedBy = "Sistema"
//         };
//         db.Usuarios.Add(usuarioAdminGeral);
//     }

//     // Executa o salvamento apenas dos registros novos que foram adicionados incrementalmente
//     await db.SaveChangesAsync();
// }


app.Run();
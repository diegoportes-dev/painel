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

//Contexto de Banco
builder.Services.AddScoped<CrudContext>();

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
app.MapPersonRoutes();
app.MapEnderecoRoutes();
app.MapPerfilRoutes();
app.MapUsuarioRoutes();
app.MapTenantRoutes();


app.Run();
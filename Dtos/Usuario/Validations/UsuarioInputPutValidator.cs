using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UsuarioInputPutValidator : AbstractValidator<(Guid id, UsuarioInputPutDto input)>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public UsuarioInputPutValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        // 1. VALIDAÇÃO DE EXISTÊNCIA: Garante que o usuário sendo editado existe
        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Usuarios.AnyAsync(u => u.Id == id, cancellation);
            })
            .WithErrorCode("NotFound") 
            .WithMessage(x => $"Usuário não encontrado.");
        
        // 2. VALIDAÇÃO DE E-MAIL: Duplicidade isolada por Tenant
        RuleFor(x => x.input.Email)
            .NotEmpty().WithMessage("O E-Mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.")  
            .MustAsync(async (model, email, cancellation) =>
            {
                var dadosOperador = await ObterDadosOperadorLogadoAsync(cancellation);
                if (dadosOperador == null) return false;

                
                // Usuário comum checa duplicidade APENAS dentro do escopo da sua empresa
                var existeNoTenant = await _db.Usuarios
                    .AnyAsync(u => u.Email == email && u.TenantId == dadosOperador.TenantId && u.Id != model.id, cancellation);
                    
                return !existeNoTenant;
                
            })
            .WithMessage(x => $"O E-Mail {x.input.Email} já está em uso por outro usuário nesta empresa.");

        // 3. VALIDAÇÃO DE SENHA ALTERNATIVA: Mantém as regras do seu projeto original
        RuleFor(x => x.input.NovaSenha)
            .Length(6, 8).WithMessage("A Nova Senha do usuário deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A Nova Senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")  
            .When(x => !string.IsNullOrEmpty(x.input.NovaSenha));

        // 4. VALIDAÇÃO DE PERFIL: Impede associação de perfis de outras empresas
        RuleFor(x => x.input.PerfilId)
            .NotEmpty().WithMessage("O Perfil é obrigatório.")
            .MustAsync(async (model, perfilId, cancellation) =>
            {
                var dadosOperador = await ObterDadosOperadorLogadoAsync(cancellation);
                if (dadosOperador == null) return false;

                // Operador comum só vincula perfis que pertençam ao seu próprio Tenant
                var existeNoTenant =  await _db.Perfis
                    .AnyAsync(p => p.Id == perfilId && p.TenantId == dadosOperador.TenantId, cancellation);

                return existeNoTenant;
              
            })
            .WithMessage(x => $"Perfil não encontrado ou não pertence a esta empresa.");
    }

    // Método utilitário privado para centralizar a descoberta do operador e evitar repetição de código
    private async Task<OperadorValidadorDto?> ObterDadosOperadorLogadoAsync(CancellationToken cancellation)
    {
        var logadoUserIdClaim = _httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(logadoUserIdClaim, out var logadoUserId)) return null;

        var info = await _db.Usuarios
            .Where(u => u.Id == logadoUserId)
            .Select(u => new OperadorValidadorDto 
            { 
                TenantId = u.TenantId, 
                IsMaster = u.Master ?? false 
            })
            .FirstOrDefaultAsync(cancellation);

        return info;
    }

    // Record auxiliar estruturado internamente
    private record OperadorValidadorDto
    {
        public Guid? TenantId { get; init; }
        public bool IsMaster { get; init; }
    }
}

using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UsuarioInputPostValidator : AbstractValidator<UsuarioInputPostDto>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioInputPostValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        
        // 1. VALIDAÇÃO DE E-MAIL (Isolada por Tenant)
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O E-Mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.")            
            .MustAsync(async (email, cancellation) =>
            {
                var dadosOperador = await ObterDadosOperadorLogadoAsync(cancellation);
                if (dadosOperador == null) return false;

                // Usuário comum valida a duplicidade APENAS dentro do seu Tenant
                var existeNoTenant = await _db.Usuarios
                    .AnyAsync(u => u.Email == email && u.TenantId == dadosOperador.TenantId, cancellation);

                return !existeNoTenant ;
                
            })
            .WithMessage(x => $"O E-Mail {x.Email} fornecido já está em uso nesta empresa.");           

        // 2. VALIDAÇÃO DE SENHA (Complexidade padrão do seu projeto)
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A Senha do usuário é obrigatória.")
            .Length(6, 8).WithMessage("A Senha do usuário deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.");

        // 3. VALIDAÇÃO DE PERFIL (Garante que o perfil pertence ao Tenant correto)
        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("O Perfil é obrigatório.")
            .MustAsync(async (perfilId, cancellation) =>
            {
                var dadosOperador = await ObterDadosOperadorLogadoAsync(cancellation);
                if (dadosOperador == null) return false;

                if (dadosOperador.IsMaster)
                {
                    // Usuário Master pode associar qualquer perfil que exista no sistema central
                    return await _db.Perfis.AnyAsync(p => p.Id == perfilId, cancellation);
                }
                else
                {
                    // Usuário comum só pode associar perfis criados para a sua PRÓPRIA empresa
                    return await _db.Perfis
                        .AnyAsync(p => p.Id == perfilId && p.TenantId == dadosOperador.TenantId, cancellation);
                }
            })
            .WithMessage(x => $"Perfil não encontrado ou não pertence a esta empresa.");
    }    

    // Método utilitário privado para centralizar a descoberta do operador
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

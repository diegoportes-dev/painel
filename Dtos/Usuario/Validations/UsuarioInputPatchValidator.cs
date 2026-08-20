using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class UsuarioInputPatchValidator : AbstractValidator<(Guid Id, UsuarioInputPatchDto Input)>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioInputPatchValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        // 1. VALIDAÇÃO DE EXISTÊNCIA: Garante que o usuário sendo alterado existe no catálogo
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Usuarios.AnyAsync(u => u.Id == id, cancellation);
            })
            .WithErrorCode("NotFound")
            .WithMessage(x => $"Usuário não encontrado.");

        // 2. VALIDAÇÃO DE E-MAIL PARCIAL: Duplicidade isolada por Tenant (se enviado)
        RuleFor(x => x.Input.Email)
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.")
            .MustAsync(async (model, email, cancellation) =>
            {
                var dadosOperador = await ObterDadosOperadorLogadoAsync(cancellation);
                if (dadosOperador == null) return false;

                
                // Operador comum (inclusive Admin) valida a duplicidade APENAS dentro do seu TenantId
                var existeNoTenant = await _db.Usuarios
                    .AnyAsync(u => u.Email == email && u.TenantId == dadosOperador.TenantId && u.Id != model.Id, cancellation);
                
                return !existeNoTenant;
                
            })
            .WithMessage(x => $"O E-Mail {x.Input.Email} já está em uso por outro usuário nesta empresa.")
            .When(x => !string.IsNullOrEmpty(x.Input.Email));

        // 3. VALIDAÇÃO DE SENHA PARCIAL: Mantém as regras de complexidade originais do seu projeto
        RuleFor(x => x.Input.NovaSenha)
            .Length(6, 8).WithMessage("A Senha do usuário deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")
            .When(x => !string.IsNullOrEmpty(x.Input.NovaSenha)); 
            
        // 4. VALIDAÇÃO DE PERFIL PARCIAL: Garante governança de permissões entre locatários (se enviado)
        RuleFor(x => x.Input.PerfilId)
            .MustAsync(async (perfilId, cancellation) =>
            {
                var dadosOperador = await ObterDadosOperadorLogadoAsync(cancellation);
                if (dadosOperador == null) return false;

                return await _db.Perfis
                    .AnyAsync(p => p.Id == perfilId && p.TenantId == dadosOperador.TenantId, cancellation);
                
            })
            .WithErrorCode("PerfilNotFound")
            .WithMessage(x => $"Perfil não encontrado ou não pertence a esta empresa.")
            .When(x => x.Input.PerfilId != null && x.Input.PerfilId != Guid.Empty);
    }

    // Método utilitário privado para centralizar a descoberta e leitura do operador logado via JWT
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

    private record OperadorValidadorDto
    {
        public Guid? TenantId { get; init; }
        public bool IsMaster { get; init; }
    }
}

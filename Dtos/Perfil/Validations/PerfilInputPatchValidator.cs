using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class PerfilInputPatchValidator : AbstractValidator<(Guid id, PerfilInputPatchDto input)>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PerfilInputPatchValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        // 1. Valida se o registro sendo modificado existe no catálogo central
        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(p => p.Id == id, cancellation);
            })
            .WithErrorCode("NotFound")
            .WithMessage(x => $"Perfil não encontrado.");

        // 2. Valida a duplicidade de nome em atualização parcial de forma isolada por Tenant
        RuleFor(x => x.input.Nome)
            .Length(3, 50).WithMessage("O Nome do Perfil deve ter entre 3 e 50 caracteres.")
            .MustAsync(async (contexto, nome, cancellation) =>
            {
                // Extrai o ID do usuário operador logado do Token JWT
                var logadoUserIdClaim = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(logadoUserIdClaim, out var logadoUserId))
                {
                    return false; // Bloqueia caso o operador seja inválido
                }

                // Coleta dados de privilégio e escopo do banco central
                var operadorInfo = await _db.Usuarios
                    .Where(u => u.Id == logadoUserId)
                    .Select(u => new { u.TenantId, IsMaster = u.Master })
                    .FirstOrDefaultAsync(cancellation);

                if (operadorInfo == null) return false;

                bool ehMaster = operadorInfo.IsMaster ?? false;
                Guid? tenantIdOperador = operadorInfo.TenantId;

                // Se for usuário de empresa, valida se já existe esse perfil DENTRO DA EMPRESA DELE
                var existeNoTenant = await _db.Perfis
                    .AnyAsync(p => p.Nome == nome && p.TenantId == tenantIdOperador, cancellation);
                
                if(existeNoTenant) return false;

                return true;
            })
            .WithMessage(x => $"O Perfil com o nome '{x.input.Nome}' já está cadastrado nesta empresa.")
            .When(x => !string.IsNullOrEmpty(x.input.Nome)); // Só executa se o nome for enviado no PATCH

        // 3. Valida a formatação de ativação aceitando apenas 'S' ou 'N' de forma flexível
        RuleFor(x => x.input.Ativo)            
            .Matches(@"^[SsNn]$")
            .WithMessage("O status deve ser obrigatoriamente 'S' (Sim) ou 'N' (Não).")
            .When(x => !string.IsNullOrEmpty(x.input.Ativo));
    }
}

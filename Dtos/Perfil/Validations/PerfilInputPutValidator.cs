using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class PerfilInputPutValidator : AbstractValidator<(Guid id, PerfilInputPutDTO input)>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PerfilInputPutValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        // 1. Valida se o registro sendo editado realmente existe no banco central
        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(x => x.Id == id, cancellation);
            })
            .WithErrorCode("NotFound")
            .WithMessage(x => $"Perfil não encontrado.");

        // 2. Valida a duplicidade de nome isolada por escopo (Tenant ou Global)
        RuleFor(x => x.input.Nome)
            .NotEmpty().WithMessage("O Nome do Perfil é obrigatório.")
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
                    .AnyAsync(p => p.Nome == nome && p.TenantId == tenantIdOperador && p.Id != contexto.id, cancellation);
                
                if(existeNoTenant) return false;

                return true;

            })
            .WithMessage(x => $"O Perfil com o nome '{x.input.Nome}' já está cadastrado nesta empresa.");
    }
}

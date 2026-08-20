using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class PerfilInputPostValidator : AbstractValidator<PerfilInputPostDTO>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PerfilInputPostValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O Nome do Perfil é obrigatório.")
            .Length(3, 50).WithMessage("O Nome do Perfil deve ter entre 3 e 50 caracteres.")
            .MustAsync(async (nome, cancellation) =>
            {
                // 1. Extrai o ID do usuário operador logado a partir do HttpContext/JWT
                var logadoUserIdClaim = _httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!Guid.TryParse(logadoUserIdClaim, out var logadoUserId))
                {
                    return false; // Bloqueia se o usuário não puder ser identificado
                }

                // 2. Coleta o TenantId e se é Master do banco central
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
            .WithMessage(x => $"O Perfil com o nome '{x.Nome}' já está cadastrado nesta empresa.");
    }
}

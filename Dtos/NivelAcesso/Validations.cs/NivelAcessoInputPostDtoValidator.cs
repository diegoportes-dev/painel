using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class NivelAcessoInputPostDtoValidator : AbstractValidator<NivelAcessoInputPostDTO>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NivelAcessoInputPostDtoValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        // 1. Regra para o PerfilId (Garante existência)
        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("O ID do Perfil é obrigatório.")
            .MustAsync(async (perfilId, cancellationToken) => 
                await _db.Perfis.AnyAsync(p => p.Id == perfilId, cancellationToken))
            .WithMessage("O Perfil informado não existe no sistema.")
            .WithErrorCode("NotFound");

        // 2. Regra para o RotaId (Garante existência)
        RuleFor(x => x.RotaId)
            .NotEmpty().WithMessage("O ID da Rota é obrigatório.")
            .MustAsync(async (rotaId, cancellationToken) => 
                await _db.Rotas.AnyAsync(r => r.Id == rotaId, cancellationToken))
            .WithMessage("A Rota informada não existe no sistema.")
            .WithErrorCode("NotFound");

        // 3. REGRA DE DUPLICIDADE: Valida a combinação única de Perfil + Rota por Tenant
        RuleFor(x => x)
            .MustAsync(async (dto, cancellationToken) =>
            {
                // Captura o TenantId do token do usuário logado
                var user = _httpContextAccessor.HttpContext?.User;
                var tenantIdClaim = user?.FindFirst("TenantId")?.Value;

                if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    // Se não conseguir ler o tenant, a rota principal rejeitará como 401,
                    // mas por segurança retornamos true aqui para não mascarar o erro de auth.
                    return true; 
                }

                // Verifica se já existe um registro idêntico para a mesma empresa
                var jaExisteCombinacao = await _db.NiveisAcessos
                    .AnyAsync(n => n.PefilId == dto.PerfilId 
                                && n.RotaId == dto.RotaId 
                                && n.TenantId == tenantId, 
                             cancellationToken);

                // Retorna TRUE se NÃO existir (validação passa), ou FALSE se já existir (dispara erro)
                return !jaExisteCombinacao;
            })
            .WithMessage("Este Perfil já possui permissão de acesso cadastrada para a Rota informada.")
            .WithName("CombinacaoPerfilRota");
    }
}

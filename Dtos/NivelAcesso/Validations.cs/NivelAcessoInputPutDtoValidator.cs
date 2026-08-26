using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

public class NivelAcessoInputPutDtoValidator : AbstractValidator<(Guid Id, NivelAcessoInputPutDTO Input)>
{
    private readonly CrudContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NivelAcessoInputPutDtoValidator(CrudContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;

        // 1. Valida se o registro principal de Nível de Acesso existe na base
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("O ID do nível de acesso é obrigatório.")
            .MustAsync(async (id, cancellationToken) => 
                await _db.NiveisAcessos.AnyAsync(n => n.Id == id, cancellationToken))
            .WithMessage("Nível de acesso não localizado no sistema.")
            .WithErrorCode("NotFound");

        // 2. Regra para o PerfilId informado no Payload (Garante existência)
        RuleFor(x => x.Input.PerfilId)
            .NotEmpty().WithMessage("O ID do Perfil é obrigatório.")
            .MustAsync(async (perfilId, cancellationToken) => 
                await _db.Perfis.AnyAsync(p => p.Id == perfilId, cancellationToken))
            .WithMessage("O Perfil informado para atualização não existe.")
            .WithErrorCode("NotFound");

        // 3. Regra para o RotaId informado no Payload (Garante existência)
        RuleFor(x => x.Input.RotaId)
            .NotEmpty().WithMessage("O ID da Rota é obrigatório.")
            .MustAsync(async (rotaId, cancellationToken) => 
                await _db.Rotas.AnyAsync(r => r.Id == rotaId, cancellationToken))
            .WithMessage("A Rota informada para atualização não existe.")
            .WithErrorCode("NotFound");

        // 4. REGRA DE DUPLICIDADE: Valida a combinação única de Perfil + Rota por Tenant (Ignorando o ID atual)
        RuleFor(x => x)
            .MustAsync(async (tupla, cancellationToken) =>
            {
                // Captura o TenantId do token do usuário logado
                var user = _httpContextAccessor.HttpContext?.User;
                var tenantIdClaim = user?.FindFirst("TenantId")?.Value;

                if (string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
                {
                    return true; 
                }

                // Verifica se JÁ EXISTE OUTRO registro com a mesma combinação para a mesma empresa
                var jaExisteCombinacao = await _db.NiveisAcessos
                    .AnyAsync(n => n.Id != tupla.Id // ⚠️ Garante que não vai colidir com o próprio registro atual
                                && n.PefilId == tupla.Input.PerfilId 
                                && n.RotaId == tupla.Input.RotaId 
                                && n.TenantId == tenantId, 
                             cancellationToken);

                // Retorna TRUE se a combinação estiver livre (válida), ou FALSE se colidir com outro registro
                return !jaExisteCombinacao;
            })
            .WithMessage("Este Perfil já possui permissão de acesso cadastrada para a Rota informada.")
            .WithName("CombinacaoPerfilRota");
    }
}

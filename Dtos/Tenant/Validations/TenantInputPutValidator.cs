using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

namespace Crud.Dtos.Tenant.Validations;

public class TenantInputPutValidator : AbstractValidator<(Guid id, TenantInputPutDto input)>
{
    private readonly CrudContext _db;

    public TenantInputPutValidator(CrudContext db)
    {
        _db = db;

        // 1. Regra prioritária: Verifica se o Tenant realmente existe no banco de catálogo central
        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Tenants.AnyAsync(t => t.Id == id, cancellation);
            })
            .WithMessage(x => $"Tenant com ID {x.id} não encontrado.")
            .WithErrorCode("NotFound"); // Código capturado no endpoint para responder 404 Not Found

        // 2. Validações dos dados enviados no corpo da requisição (input)
        RuleFor(x => x.input.Nome)
            .NotEmpty().WithMessage("O Nome/Razão Social é obrigatório.")
            .MaximumLength(150).WithMessage("O Nome não pode exceder 150 caracteres.");

        RuleFor(x => x.input.Documento)
            .NotEmpty().WithMessage("O documento (CPF/CNPJ) é obrigatório.")
            .Must((contexto, documento) => ValidarFormatoDocumento(documento, contexto.input.Tipo))
            .WithMessage("O formato do documento fornecido é inválido para o Tipo selecionado.")
            .MustAsync(async (contexto, documento, cancellation) =>
            {
                var docLimpo = new string(documento.Where(char.IsDigit).ToArray());
                // Permite manter o próprio documento atual, mas impede a colisão com o documento de outros tenants
                var docExiste = await _db.Tenants
                    .AnyAsync(t => t.Documento == docLimpo && t.Id != contexto.id, cancellation);
                return !docExiste;
            })
            .WithMessage("Este CPF/CNPJ já possui um cadastro ativo no sistema.");
            
        RuleFor(x => x.input.Ativo)
            .NotEmpty().WithMessage("O status Ativo é obrigatório.");
    }

    private bool ValidarFormatoDocumento(string documento, TipoTenant tipo)
    {
        if (string.IsNullOrWhiteSpace(documento)) return false;
        
        var docLimpo = new string(documento.Where(char.IsDigit).ToArray());
        if (tipo == TipoTenant.PessoaFisica && docLimpo.Length == 11) return true;
        if (tipo == TipoTenant.PessoaJuridica && docLimpo.Length == 14) return true;
        
        return false;
    }
}

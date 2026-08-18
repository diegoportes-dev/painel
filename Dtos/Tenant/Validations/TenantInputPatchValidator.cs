using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using System.Data;
using System.Reflection.Metadata;

namespace Crud.Dtos.Tenant.Validations;

public class TenantInputPatchValidator : AbstractValidator<(Guid id, TenantInputPatchDto input)>
{
    private readonly CrudContext _db;

    public TenantInputPatchValidator(CrudContext db)
    {
        _db = db;

        // 1. Regra prioritária: Verifica se o Tenant existe no banco central
        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Tenants.AnyAsync(t => t.Id == id, cancellation);
            })
            .WithMessage(x => $"Tenant com ID {x.id} não encontrado.")
            .WithErrorCode("NotFound");

        // 2. Validações condicionais (só validam se o campo não vier nulo na requisição)
        RuleFor(x => x.input.Nome)
            .NotEmpty().WithMessage("O Nome/Razão Social não pode ser vazio.")
            .MaximumLength(150).WithMessage("O Nome não pode exceder 150 caracteres.")
            .When(x => x.input.Nome != null);

        RuleFor(x => x.input.Documento)
            .NotEmpty().WithMessage("O documento (CPF/CNPJ) não pode ser vazio.")
            .Must((contexto, documento) => 
            {
                // Se o tipo mudou na mesma requisição, valida contra o novo tipo. Senão, assume que o tipo atual é mantido.
                var tipoValidacao = contexto.input.Tipo ?? TipoTenant.PessoaFisica; 
                var docLimpo = new string(documento!.Where(char.IsDigit).ToArray());
                
                if (tipoValidacao == TipoTenant.PessoaFisica && docLimpo.Length == 11) return true;
                if (tipoValidacao == TipoTenant.PessoaJuridica && docLimpo.Length == 14) return true;
                return false;
            })
            .WithMessage("O formato do documento fornecido é inválido para o Tipo selecionado.")
            .MustAsync(async (contexto, documento, cancellation) =>
            {
                var docLimpo = new string(documento!.Where(char.IsDigit).ToArray());
                var docExiste = await _db.Tenants
                    .AnyAsync(t => t.Documento == docLimpo && t.Id != contexto.id, cancellation);
                return !docExiste;
            })
            .WithMessage("Este CPF/CNPJ já possui um cadastro ativo no sistema.")
            .When(x => x.input.Documento != null);

        RuleFor(x => x.input.Tipo)
        .MustAsync(async (contexto, tipoInserido, cancellation) =>
        {            
            if (contexto.input.Documento != null) return true;

            var documentoBanco = await _db.Tenants
                .Where(t => t.Id == contexto.id)
                .Select(t => t.Documento)
                .FirstOrDefaultAsync(cancellation);

            if (string.IsNullOrEmpty(documentoBanco)) return false;

            var docLimpo = new string(documentoBanco.Where(char.IsDigit).ToArray());

            if (tipoInserido == TipoTenant.PessoaFisica && docLimpo.Length == 11) return true;
            if (tipoInserido == TipoTenant.PessoaJuridica && docLimpo.Length == 14) return true;

            return false;
        })
        .WithMessage("O formato do documento atual em banco é inválido para o novo Tipo selecionado.")
        .When(x => x.input.Tipo != null); // Só executa se o Tipo vier no PATCH

            
        RuleFor(x => x.input.Ativo)
            .NotEmpty().WithMessage("O campo Ativo não pode ser vazio.")
            .When(x => x.input.Ativo != null);
    }
}

using Crud.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

public class TenantInputPostValidator : AbstractValidator<TenantInputPostDto>
{
    private readonly CrudContext _db;

    public TenantInputPostValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O Nome/Razão Social é obrigatório.")
            .MaximumLength(150).WithMessage("O Nome não pode exceder 150 caracteres.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("O Slug de identificação é obrigatório.")
            .MaximumLength(50).WithMessage("O Slug não pode exceder 50 caracteres.")
            .Matches(@"^[a-z0-Validation\d-]+$").WithMessage("O Slug deve conter apenas letras minúsculas, números e hifens.")
            .MustAsync(async (slug, cancellation) =>
            {
                // Garante que nenhuma outra empresa use o mesmo endereço/slug no sistema central
                var slugExiste = await _db.Tenants.AnyAsync(t => t.Slug == slug.ToLower().Trim(), cancellation);
                return !slugExiste;
            })
            .WithMessage(x => $"O identificador '{x.Slug}' já está sendo utilizado por outra empresa.");

        RuleFor(x => x.Documento)
            .NotEmpty().WithMessage("O documento (CPF/CNPJ) é obrigatório.")
            .Must((dto, documento) => ValidarFormatoDocumento(documento, dto.Tipo))
            .WithMessage("O formato do documento fornecido é inválido para o Tipo selecionado.")
            .MustAsync(async (documento, cancellation) =>
            {
                var docLimpo = LimparDocumento(documento);
                var docExiste = await _db.Tenants.AnyAsync(t => t.Documento == docLimpo, cancellation);
                return !docExiste;
            })
            .WithMessage("Este CPF/CNPJ já possui um cadastro ativo no sistema.");
    }

    private bool ValidarFormatoDocumento(string documento, TipoTenant tipo)
    {
        var docLimpo = LimparDocumento(documento);

        if (tipo == TipoTenant.PessoaFisica && docLimpo.Length == 11) return true;
        if (tipo == TipoTenant.PessoaJuridica && docLimpo.Length == 14) return true;

        return false;
    }

    private string LimparDocumento(string documento)
    {
        return new string(documento.Where(char.IsDigit).ToArray());
    }
}

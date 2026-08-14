using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using Microsoft.AspNetCore.Components.Forms;

public class PerfilInputPatchValidator : AbstractValidator<(Guid id, PerfilInputPatchDto input)>
{
    private readonly CrudContext _db;

    public PerfilInputPatchValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(p => p.Id == id, cancellation);
            })
            .WithErrorCode("NotFound")
            .WithMessage(x => $"Perfil não encontrado.");

        RuleFor(x => x.input.Nome)
            .Length(3, 50).WithMessage("O Nome do Perfil deve ter entre 3 e 50 caracteres.")
            .MustAsync(async (model, nome, cancellation) =>
            {
                var existNome = await _db.Perfis
                    .AnyAsync(p => p.Nome == nome && p.Id != model.id, cancellation);
                return !existNome;
            }).WithMessage(x => $"Perfil {x.input.Nome} já existe.")
            .When(x => !string.IsNullOrEmpty(x.input.Nome));

        RuleFor(x => x.input.Ativo)            
            .Matches(@"^[SsNn]$")
            .WithMessage("O status deve ser obrigatoriamente 'S' (Sim) ou 'N' (Não).")
            .When(x => !string.IsNullOrEmpty(x.input.Ativo));

    }
}
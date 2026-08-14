using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;
using System.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http.HttpResults;

public class PerfilInputPutValidator : AbstractValidator<(Guid id, PerfilInputPutDTO input)>
{
    private readonly CrudContext _db;

    public PerfilInputPutValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.id )
            .MustAsync( async(id, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(x => x.Id == id, cancellation);
            })
            .WithErrorCode("NotFound")
            .WithMessage(x => $"Perfil não encontrado.");

        RuleFor(x => x.input.Nome)
            .NotEmpty().WithMessage("O Nome do Perfil é obrigatório.")
            .Length(3, 50).WithMessage("O Nome do Perfil deve ter entre 3 e 50 caracteres.")
            .MustAsync(async (model, nome, cancellation) =>
            {
                return !await _db.Perfis.AnyAsync(p => p.Nome == nome && p.Id != model.id, cancellation);
            }).WithMessage(x => $"Perfil {x.input.Nome} já existe.");
    }
}
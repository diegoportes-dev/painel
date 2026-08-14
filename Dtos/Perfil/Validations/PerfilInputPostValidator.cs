using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

public class PerfilInputPostValidator : AbstractValidator<PerfilInputPostDTO>
{
    private readonly CrudContext _db;

    public PerfilInputPostValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O Nome do Perfil é obrigatório.")
            .Length(3, 50).WithMessage("O Nome do Perfil deve ter entre 3 e 50 caracteres.")
            .MustAsync(async (nome, cancellation) =>
            {
                return !await _db.Perfis.AnyAsync(p => p.Nome == nome, cancellation);
            }).WithMessage(x => $"Perfil já existente.");
    }
}
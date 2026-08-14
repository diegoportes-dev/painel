using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

public class ResetarSenhaInputValidator : AbstractValidator<ResetarSenhaInputDto>
{
    private readonly CrudContext _db;

    public ResetarSenhaInputValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O E-Mail é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("O Token é obrigatório.");

        RuleFor(x => x.NovaSenha)
            .NotEmpty().WithMessage("A Nova Senha é obrigatória.")
            .Length(6, 8).WithMessage("A Senha deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.");
    }
}

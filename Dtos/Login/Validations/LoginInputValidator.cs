using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;


public class LoginInputValidator : AbstractValidator<LoginInputDto>
{
    private readonly CrudContext _db;

    public LoginInputValidator( CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O E-Mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.");
      
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A Senha do usuário é obrigatória.")
            .Length(6, 8).WithMessage("A Senha do usuário deve ter entre 6 e 8 caracteres.")
            // Executa exatamente a mesma expressão regular complexa do seu DataAnnotation
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.");
    }

    
}
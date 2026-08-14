using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;


public class EsqueciSenhaInputValidator : AbstractValidator<EsqueciSenhaInputDto>
{
    private readonly CrudContext _db;

    public EsqueciSenhaInputValidator( CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O E-Mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.");
      
    }

    
}
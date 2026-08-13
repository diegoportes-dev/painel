using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

public class UsuarioInputPostDtoValidator : AbstractValidator<UsuarioInputPostDto>
{
    private readonly CrudContext _db;

    public UsuarioInputPostDtoValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O E-Mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.")            
            .MustAsync(async (email, cancellation) =>
            {
                var existEmail = await _db.Usuarios.AnyAsync(u => u.Email == email, cancellation);
                return !existEmail;
            })
            .WithMessage(x => $"O E-Mail {x.Email} fornecido já está em uso.");           

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A Senha do usuário é obrigatória.")
            .Length(6, 8).WithMessage("A Senha do usuário deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.");

        RuleFor(x => x.PerfilId)
            .NotEmpty().WithMessage("O Identificador da Pessoa é obrigatório.")
            .MustAsync(async (perfilId, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(p => p.Id == perfilId, cancellation);
            })
            .WithMessage(x => $"Perfil com ID {x.PerfilId} não encontrado.");
    }    
}
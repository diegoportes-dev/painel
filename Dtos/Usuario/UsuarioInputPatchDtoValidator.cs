using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

public class UsuarioInputPatchDtoValidator : AbstractValidator<(Guid Id, UsuarioInputPatchDto Input)>
{
    private readonly CrudContext _db;

    public UsuarioInputPatchDtoValidator(CrudContext db)
    {
        _db = db;

        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Usuarios.AnyAsync(u => u.Id == id, cancellation);
            })
            .WithErrorCode("NotFound")
            .WithMessage(x => $"Usuário não encontrado.");

        RuleFor(x => x.Input.Email)
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.")
            .MustAsync(async (model, email, cancellation) =>
            {
                var existEmail = await _db.Usuarios
                    .AnyAsync(u => u.Email == email && u.Id != model.Id, cancellation);
                return !existEmail;
            }).WithMessage(x => $"E-Mail {x.Input.Email} já está em uso por outro usuário.")
            .When(x => !string.IsNullOrEmpty(x.Input.Email));

        RuleFor(x => x.Input.NovaSenha)
            .Length(6, 8).WithMessage("A Senha do usuário deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")
            .When(x => !string.IsNullOrEmpty(x.Input.NovaSenha)); 
            
        RuleFor(x => x.Input.PerfilId)
            .MustAsync(async (perfilId, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(p => p.Id == perfilId, cancellation);
            })
            .WithErrorCode("PerfilNotFound")
            .WithMessage(x => $"Perfil não encontrado.")
            .When(x => x.Input.PerfilId != Guid.Empty); // Só valida se enviou um Guid válido
    }
}

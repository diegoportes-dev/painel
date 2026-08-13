using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

public class UsuarioInputPutDtoValidator :  AbstractValidator<(Guid id, UsuarioInputPutDto input)>
{
    private readonly CrudContext _db;
    
    public UsuarioInputPutDtoValidator(CrudContext db){
        _db = db;

        RuleFor(x => x.id)
            .MustAsync(async (id, cancellation) =>
            {
                return await _db.Usuarios.AnyAsync(u => u.Id == id, cancellation);
            })
            .WithErrorCode("NotFound") 
            .WithMessage(x => $"Usuário não encontrado.");
        
        RuleFor(x => x.input.Email)
            .NotEmpty().WithMessage("O E-Mail do usuário é obrigatório.")
            .EmailAddress().WithMessage("O E-Mail fornecido não é válido.")
            .MaximumLength(100).WithMessage("O E-Mail do usuário não pode exceder 100 caracteres.")  
            .MustAsync( async( model, email, cancellation) =>
            {
                var existEmail = await _db.Usuarios.AnyAsync(u => u.Email == email && u.Id != model.id , cancellation);
                return !existEmail;
            }).WithMessage(x => $"E-Mail {x.input.Email} já está em uso por outro usuário.");

        RuleFor(x => x.input.NovaSenha)
            .Length(6, 8).WithMessage("A Nova Senha do usuário deve ter entre 6 e 8 caracteres.")
            .Matches(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$")
            .WithMessage("A Nova Senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")  
            .When(x => !string.IsNullOrEmpty(x.input.NovaSenha));

        RuleFor(x => x.input.PerfilId)
            .NotEmpty().WithMessage("O Perfil é obrigatório.")
            .MustAsync(async (model, perfilId, cancellation) =>
            {
                return await _db.Perfis.AnyAsync(p => p.Id == perfilId, cancellation);
            })
            .WithMessage(x => $"Perfil não encontrado.");
    }
    
}
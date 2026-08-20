using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Crud.Data;

public class PerfilDeleteValidator : AbstractValidator<Guid>
{
    private readonly CrudContext _db;

    public PerfilDeleteValidator(CrudContext db)
    {
        _db = db;

        // Regra 1: Verifica se o perfil existe
        RuleFor(id => id)
            .MustAsync(async (id, cancellation) => await _db.Perfis.AnyAsync(p => p.Id == id, cancellation))
            .WithErrorCode("NotFound")
            .WithMessage("Perfil não encontrado.");

        // Regra 2: Bloqueia a exclusão se houver usuários vinculados
        RuleFor(id => id)
            .MustAsync(async (id, cancellation) =>
            {
                // Verifica se existe QUALQUER usuário usando este PerfilId
                var possuiUsuarios = await _db.Usuarios.AnyAsync(u => u.PerfilId == id, cancellation);
                return !possuiUsuarios; // Retorna true (válido) se NÃO possuir usuários
            })
            .WithMessage("Não é possível excluir este perfil porque existem usuários vinculados a ele.");
    }
}

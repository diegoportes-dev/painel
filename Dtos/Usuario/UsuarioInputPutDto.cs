using System.ComponentModel.DataAnnotations;

public class UsuarioInputPutDto
{
    [Required(ErrorMessage = "O E-Mail do usuário é obrigatório.")]
    [EmailAddress(ErrorMessage = "O E-Mail fornecido não é válido.")]
    [StringLength(100, ErrorMessage = "O E-Mail do usuário não pode exceder 100 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(8, MinimumLength = 6, ErrorMessage = "A Nova Senha do usuário deve ter entre 6 e 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$", ErrorMessage = "A Nova Senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")]
    public string? NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Identificador da Pessoa é obrigatório.")]
    [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", ErrorMessage = "O Identificador da Pessoa deve ser válido.")]
    public Guid PerfilId { get; set; }
    // public PerfilModel Perfil { get; set; } = null!; 

    public string? Ativo {get; set;}

    public UsuarioInputPutDto(){}
    public UsuarioInputPutDto(UsuarioModel item) => ( Email, NovaSenha, PerfilId, Ativo) = ( item.Email, item.SenhaCrypt , item.PerfilId, item.Ativo);
}

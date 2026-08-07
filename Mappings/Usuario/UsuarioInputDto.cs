using System.ComponentModel.DataAnnotations;

public class UsuarioInputDto
{
    [Required(ErrorMessage = "O E-Mail do usuário é obrigatório.")]
    [EmailAddress(ErrorMessage = "O E-Mail fornecido não é válido.")]
    [StringLength(100, ErrorMessage = "O E-Mail do usuário não pode exceder 100 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Senha do usuário é obrigatória.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "A Senha do usuário deve ter entre 6 e 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$", ErrorMessage = "A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")]
    public string SenhaHash { get; set; } = string.Empty;

    
    [Required(ErrorMessage = "O Identificador da Pessoa é obrigatório.")]
    [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", ErrorMessage = "O Identificador da Pessoa deve ser válido.")]
    public Guid PerfilId { get; set; }
    // public PerfilModel Perfil { get; set; } = null!; 

    public UsuarioInputDto(){}
    public UsuarioInputDto(UsuarioModel item) => ( Email, SenhaHash, PerfilId) = ( item.Email, item.SenhaHash, item.PerfilId);
}
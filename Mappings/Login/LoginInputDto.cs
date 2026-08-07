// public record LoginIputDto(string Email, string SenhaHash);
// public record LoginIputDto(string Email, string SenhaHash, string Status);

using System.ComponentModel.DataAnnotations;

public class LoginInputDto
{
    [Required(ErrorMessage = "O E-Mail do usuário é obrigatório.")]
    [EmailAddress(ErrorMessage = "O E-Mail fornecido não é válido.")]
    [StringLength(100, ErrorMessage = "O E-Mail do usuário não pode exceder 100 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Senha do usuário é obrigatória.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "A Senha do usuário deve ter entre 6 e 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$", ErrorMessage = "A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")]
    public string SenhaHash { get; set; } = string.Empty;

    public LoginInputDto(){}
    public LoginInputDto(UsuarioModel item) => ( Email, SenhaHash) = ( item.Email, item.SenhaHash);
}
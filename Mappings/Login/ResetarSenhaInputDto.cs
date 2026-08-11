using System.ComponentModel.DataAnnotations;

public class ResetarSenhaInputDto
{
    [Required(ErrorMessage = "O E-Mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O E-Mail fornecido não é válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "O Token é obrigatório.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "A Nova Senha é obrigatória.")]
    [StringLength(8, MinimumLength = 6, ErrorMessage = "A Senha deve ter entre 6 e 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{6,8}$", ErrorMessage = "A senha deve conter pelo menos uma letra maiúscula, uma minúscula e um número.")]
    public string NovaSenha { get; set; } = string.Empty;
}
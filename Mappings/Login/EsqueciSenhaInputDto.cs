using System.ComponentModel.DataAnnotations;

public class EsqueciSenhaInputDto
{
    [Required(ErrorMessage = "O E-Mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "O E-Mail fornecido não é válido.")]
    public string Email { get; set; } = string.Empty;
}
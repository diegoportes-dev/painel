using System.ComponentModel.DataAnnotations;

public class EnderecoPatchDto
{
    [StringLength(100, MinimumLength = 3, ErrorMessage = "O Endereco deve ter entre 3 e 100 caracteres.")]
    public string? Endereco { get; set; }

    [StringLength(10, MinimumLength = 1, ErrorMessage = "O Numero deve ter entre 1 e 10 caracteres.")]
    public string? Numero { get; set; }

    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    [StringLength(9, MinimumLength = 8, ErrorMessage = "O Cep deve ter entre 8 e 9 caracteres.")]
    public string? Cep { get; set; }

    [RegularExpression(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", ErrorMessage = "O Identificador da Pessoa deve ser válido.")]
    public string? PersonId { get; set; }
}

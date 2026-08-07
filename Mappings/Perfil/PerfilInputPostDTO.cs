using System.ComponentModel.DataAnnotations;

public class PerfilInputPostDTO
{
    [Required(ErrorMessage = "O Nome do Perfil é obrigatório.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "O Nome do Perfil deve ter entre 3 e 50 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "A Descrição do Perfil não pode exceder 200 caracteres.")]
    public string? Descricao { get; set; }

    // public string? Ativo {get; set;}

    public PerfilInputPostDTO(){}
    public PerfilInputPostDTO(PerfilModel item) => ( Nome, Descricao) = ( item.Nome, item.Descricao);
}
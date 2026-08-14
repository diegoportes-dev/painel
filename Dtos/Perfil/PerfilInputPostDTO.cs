public class PerfilInputPostDTO
{    
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public PerfilInputPostDTO(){}
    public PerfilInputPostDTO(PerfilModel item) => ( Nome, Descricao) = ( item.Nome, item.Descricao);
}
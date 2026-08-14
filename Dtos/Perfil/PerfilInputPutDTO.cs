public class PerfilInputPutDTO
{   
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Ativo {get; set;}

    public PerfilInputPutDTO(){}
    public PerfilInputPutDTO(PerfilModel item) => ( Nome, Descricao, Ativo) = ( item.Nome, item.Descricao, item.Ativo);
}
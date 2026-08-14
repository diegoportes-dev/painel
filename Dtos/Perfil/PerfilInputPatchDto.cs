public class PerfilInputPatchDto
{   
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Ativo {get; set;}

    public PerfilInputPatchDto(){}
    public PerfilInputPatchDto(PerfilModel item) => ( Nome, Descricao, Ativo) = ( item.Nome, item.Descricao, item.Ativo);
}
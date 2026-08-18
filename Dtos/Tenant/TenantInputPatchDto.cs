public class TenantInputPatchDto
{
    public string? Nome { get; set; }
    public string? NomeSecundario { get; set; }
    public string? Documento { get; set; }
    public TipoTenant? Tipo { get; set; }
    public string? Ativo { get; set; }

    // Construtor vazio padrão
    public TenantInputPatchDto() {}

    // Construtor de mapeamento reverso (Model -> DTO)
    public TenantInputPatchDto(TenantModel item) => 
        (Nome, NomeSecundario, Documento, Tipo, Ativo) = 
        (item.Nome, item.NomeSecundario, item.Documento, item.Tipo, item.Ativo);
}

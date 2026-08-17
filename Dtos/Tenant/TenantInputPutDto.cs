public class TenantInputPutDto
{   
    public string Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; }
    public string Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Ativo { get; set; }

    // Construtor vazio padrão para a desserialização do JSON
    public TenantInputPutDto() {}

    // Construtor de mapeamento reverso (Model -> DTO)
    public TenantInputPutDto(TenantModel item) => 
        (Nome, NomeSecundario, Documento, Tipo, Slug, Ativo) = 
        (item.Nome, item.NomeSecundario, item.Documento, item.Tipo, item.Slug, item.Ativo);
}

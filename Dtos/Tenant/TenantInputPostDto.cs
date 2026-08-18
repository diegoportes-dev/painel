public class TenantInputPostDto
{
    
    public string Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; }
    public string Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    

    // Construtor vazio obrigatório para a desserialização do JSON da API
    public TenantInputPostDto() {}

    // Construtor de conversão (Model -> DTO) caso precise exibir os dados preenchidos
    public TenantInputPostDto(TenantModel item) => 
        (Nome, NomeSecundario, Documento, Tipo) = 
        (item.Nome, item.NomeSecundario, item.Documento, item.Tipo);
}

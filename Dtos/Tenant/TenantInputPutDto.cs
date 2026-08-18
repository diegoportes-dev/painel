public class TenantInputPutDto
{   
    public string Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; }
    public string Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    public string? Ativo { get; set; }

    public TenantInputPutDto() {}
    public TenantInputPutDto(TenantModel item) => 
        (Nome, NomeSecundario, Documento, Tipo, Ativo) = (item.Nome, item.NomeSecundario, item.Documento, item.Tipo, item.Ativo);
}
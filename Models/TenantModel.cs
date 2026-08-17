public enum TipoTenant
{
    PessoaFisica = 1,
    PessoaJuridica = 2
}

public class TenantModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; } 
    public string Documento { get; set; } = string.Empty; // CPF ou CNPJ
    public TipoTenant Tipo { get; set; }
    public string Slug { get; set; } = string.Empty; 

    // String de conexão específica deste cliente
    public string ConnectionString { get; set; } = string.Empty;

    public TenantModel() : base() { }
}

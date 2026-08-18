using Microsoft.Build.Framework;

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
    public string Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    public string Slug { get; set; } = string.Empty; 

    // Estratégia de conexão fracionada e segura
    public string DatabaseName { get; set; } = string.Empty;
    public string DbUserEncrypted { get; set; } = string.Empty;  // Criptografado com AES-256
    public string DbPasswordEncrypted { get; set; } = string.Empty;  // Criptografado com AES-256

    public TenantModel() : base() { }

    // // Invoca o construtor da classe pai antes de executar o bloco
    // public TenantModel(TenantModel input) : base()
    // {
    //     Id = Guid.NewGuid();
    //     Nome = input.Nome;
    //     NomeSecundario = input.NomeSecundario;
    //     Documento = input.Documento;
    //     Tipo = input.Tipo;
    //     Slug = input.Slug;
    //     DatabaseName = input.DatabaseName;
    //     DbUserEncrypted = input.DbUserEncrypted;
    //     DbPasswordEncrypted = input.DbPasswordEncrypted;
    // }
}

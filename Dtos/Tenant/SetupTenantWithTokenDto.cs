public class SetupTenantWithTokenDto
{
    public string TokenCadastro { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; }
    public string Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    public string SenhaDefinitiva { get; set; } = string.Empty;
}

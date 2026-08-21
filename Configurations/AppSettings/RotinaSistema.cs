using Microsoft.Extensions.Options;

public class RotinaSistema
{
    private readonly DatabaseConfig _config;

    // O ASP.NET Core injeta os dados automaticamente aqui
    public RotinaSistema(IOptions<DatabaseConfig> config)
    {
        _config = config.Value;
    }

    public void ExecutarRotina()
    {
        // Acessando as credenciais lidas do appsettings
        string banco = _config.BancoNome;
        string usuario = _config.Usuario;
        string senha = _config.Senha;

        // Prossiga com a lógica da sua rotina aqui
    }
}

public sealed record NivelAcessoRotaOutputDto
{
    public Guid Id { get; init; }
    public string EndPoint { get; init; }
    public string Rota { get; init; }
    public string Metodo { get; init; }
    public string Descricao { get; init; }
    public string Menu { get; init; }

    // Construtor padrão necessário para serialização/deserialização
    public NivelAcessoRotaOutputDto() { }

    // Construtor de mapeamento direto a partir do seu modelo de banco de dados
    public NivelAcessoRotaOutputDto(RotaModel model)
    {
        if (model == null) return;

        Id = model.Id;
        EndPoint = model.EndPoint ?? string.Empty;
        Rota = model.Rota ?? string.Empty;
        Metodo = model.Metodo ?? string.Empty;
        Descricao = model.Descricao ?? string.Empty;
        Menu = model.Menu ?? string.Empty;
    }
}

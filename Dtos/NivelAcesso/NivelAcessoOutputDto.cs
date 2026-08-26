public sealed record NivelAcessoOutputDto
{
    public Guid Id { get; init; }
    public Guid PerfilId { get; init; }
    public string PerfilNome { get; init; }
    public Guid RotaId { get; init; }
    public string RotaUrl { get; init; }
    public string RotaMetodo { get; init; }
    public string RotaDescricao { get; init; }
    public Guid? TenantId { get; init; }

    // Construtor que aceita o modelo para conversão automática (usado no .Select() do LINQ)
    public NivelAcessoOutputDto(NivelAcessoModel model)
    {
        Id = model.Id;
        PerfilId = model.PefilId; // Mapeia o PefilId (conforme sua propriedade do model)
        PerfilNome = model.Perfil?.Nome ?? "Não informado";
        RotaId = model.RotaId;
        RotaUrl = model.Rota?.Rota ?? "Não informada";
        RotaMetodo = model.Rota?.Metodo ?? "Não informado";
        RotaDescricao = model.Rota?.Descricao ?? "Não informada";
        TenantId = model.TenantId;
    }
}

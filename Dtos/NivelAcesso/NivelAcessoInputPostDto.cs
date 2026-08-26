public sealed record NivelAcessoInputPostDTO
{
    public Guid PerfilId { get; init; }
    public Guid RotaId { get; init; }

    // Construtor vazio (necessário para alguns serializadores)
    public NivelAcessoInputPostDTO() { }

    // Construtor que inicializa a partir do modelo de Nível de Acesso
    public NivelAcessoInputPostDTO(NivelAcessoModel item) 
    {
        PerfilId = item.PefilId;
        RotaId = item.RotaId;
    }
}

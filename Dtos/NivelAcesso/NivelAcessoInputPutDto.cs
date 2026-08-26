public sealed record NivelAcessoInputPutDTO
{
    public Guid PerfilId { get; init; }
    public Guid RotaId { get; init; }

    // Construtor vazio padrão
    public NivelAcessoInputPutDTO() { }

    // Construtor opcional para carregar a partir do modelo
    public NivelAcessoInputPutDTO(NivelAcessoModel item)
    {
        PerfilId = item.PefilId;
        RotaId = item.RotaId;
    }
}

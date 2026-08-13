public class UsuarioInputPatchDto
{
    public string Email { get; set; } = string.Empty;
    public string? NovaSenha { get; set; } = string.Empty;
    public Guid PerfilId { get; set; }
    // public PerfilModel Perfil { get; set; } = null!; 

    public string? Ativo {get; set;}

    public UsuarioInputPatchDto(){}
    public UsuarioInputPatchDto(UsuarioModel item) => ( Email, NovaSenha, PerfilId, Ativo) = ( item.Email, item.SenhaCrypt , item.PerfilId, item.Ativo);
}

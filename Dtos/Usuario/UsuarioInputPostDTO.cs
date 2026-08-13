public class UsuarioInputPostDto
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public Guid PerfilId { get; set; }
    
    public UsuarioInputPostDto(){}
    public UsuarioInputPostDto(UsuarioModel item) => ( Email, Senha, PerfilId) = ( item.Email, item.SenhaCrypt, item.PerfilId);
}
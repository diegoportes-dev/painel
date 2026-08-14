public class LoginInputDto
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;

    public LoginInputDto(){}
    public LoginInputDto(UsuarioModel item) => ( Email, Senha) = ( item.Email, item.SenhaCrypt );
}
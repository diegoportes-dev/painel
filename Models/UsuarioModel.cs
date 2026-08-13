public class UsuarioModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;    
    public string SenhaCrypt { get; set; } = string.Empty;  
    public Guid PerfilId { get; set; }
    public PerfilModel Perfil { get; set; } = null!; 
    public string? TokenReset { get; set; }
    public DateTime? TokenResetExpiracao { get; set; }

    public UsuarioModel():base(){}

    public UsuarioModel(UsuarioInputPostDto input):base()
    {
        Id = Guid.NewGuid();
        Email = input.Email;       
        SenhaCrypt = input.Senha;
        PerfilId = Guid.Parse(input.PerfilId.ToString());
    }

    public void UpdateUsuario(UsuarioInputPutDto input)
    {
        Email = input.Email;
        SenhaCrypt = input.NovaSenha;
        PerfilId = input.PerfilId; 
        this.Ativo = input.Ativo;

        // if (!string.IsNullOrEmpty(input.NovaSenha))
        // {
        //     SenhaCrypt = input.NovaSenha;
        // }
    }

}
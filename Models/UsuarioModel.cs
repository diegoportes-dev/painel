public class UsuarioModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;    
    public Guid PerfilId { get; set; }
    public PerfilModel Perfil { get; set; } = null!; 

    public UsuarioModel():base(){}

    public UsuarioModel(UsuarioInputPostDto input):base()
    {
        Id = Guid.NewGuid();
        Email = input.Email;
        SenhaHash = input.SenhaHash;
        PerfilId = input.PerfilId;
    }

    public void UpdateUsuario(UsuarioInputPutDto input)
    {
        Email = input.Email;
        SenhaHash = input.SenhaHash;
        PerfilId = input.PerfilId; 
        this.Ativo = input.Ativo;
    }

}
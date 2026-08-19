public class UsuarioModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;    
    public string SenhaCrypt { get; set; } = string.Empty;  
    public Guid PerfilId { get; set; }
    public PerfilModel Perfil { get; set; } = null!; 
    public string? TokenReset { get; set; }
    public DateTime? TokenResetExpiracao { get; set; }
    public string? TokenCadastro { get; set;}
    public DateTime? TokenCadastroExpiracao { get; set; }
    public Guid? TenantId { get; set; }
    public TenantModel? Tenant { get; set; }
    public Boolean? Master {get; set;} = false;

    public UsuarioModel():base(){}

    public UsuarioModel(UsuarioInputPostDto input, Guid? tenantId = null):base()
    {
        Id = Guid.NewGuid();
        Email = input.Email;       
        SenhaCrypt = input.Senha;
        PerfilId = Guid.Parse(input.PerfilId.ToString());
        TenantId = tenantId; 
    }

    public void UpdateUsuario(UsuarioInputPutDto input)
    {
        Email = input.Email;
        SenhaCrypt = input.NovaSenha;
        PerfilId = input.PerfilId; 
        this.Ativo = input.Ativo;
    }

}
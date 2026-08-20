public class PerfilModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid? TenantId { get; set;}
    public virtual TenantModel? Tenant { get; set; }

    public PerfilModel() : base() { }

    // Invoca o construtor da classe pai antes de executar o bloco
    public PerfilModel(PerfilInputPostDTO input, Guid? tenantId) : base()
    {
        Id = Guid.NewGuid();
        Nome = input.Nome;
        Descricao = input.Descricao;
        TenantId = tenantId;
    }

    public void UpdatePerfil(PerfilInputPutDTO input)
    {
        Nome = input.Nome;
        Descricao = input.Descricao; 
        this.Ativo = input.Ativo;       
    }
}

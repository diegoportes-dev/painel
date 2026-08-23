public class NivelAcessoModel : AuditoriaModel
{
    public Guid Id {get; set;}
    public Guid PefilId {get; set;}
    public PerfilModel Perfil {get; set;}
    public Guid RotaId {get; set;}
    public RotaModel Rota {get; set;}
    public Guid? TenantId { get; set; }
    public TenantModel? Tenant { get; set; }

    public NivelAcessoModel():base(){}    
}
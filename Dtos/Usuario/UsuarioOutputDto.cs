public class UsuarioOutputDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public PerfilSummaryDto Perfil { get; set; } = null!;
    public TenantSummaryDto Tenant {get; set;} = null!;
        
    //Auditoria
    public string Ativo { get; set; }     
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }    
    public DateTime? Updated { get; set; }
    public string? UpdatedBy { get; set; }

    public UsuarioOutputDto() { }
    public UsuarioOutputDto(UsuarioModel item) => (Id, Email, Perfil, Ativo, Created, CreatedBy, Updated, UpdatedBy, Tenant ) = (item.Id, item.Email, item.Perfil is null ? null : new PerfilSummaryDto(item.Perfil), item.Ativo, item.Created, item.CreatedBy, item.Updated, item.UpdatedBy, item.Tenant is null ? null : new TenantSummaryDto(item.Tenant) );
  
}

public class PerfilSummaryDto
    {
        public Guid? Id { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }

        public PerfilSummaryDto() { }
        public PerfilSummaryDto(PerfilModel perfil)
        {
            Id = perfil.Id;
            Nome = perfil.Nome;
            Descricao = perfil.Descricao;
        }
    }

public class TenantSummaryDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; }
    public string Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    public string Slug { get; set; } = string.Empty;

    public TenantSummaryDto(){}
    public TenantSummaryDto(TenantModel tenant)
    {
        Id = tenant.Id;
        Nome = tenant.Nome;
        NomeSecundario = tenant.NomeSecundario;
        Documento = tenant.Documento;
        Tipo = tenant.Tipo;
        Slug  = tenant.Slug;
    }
}
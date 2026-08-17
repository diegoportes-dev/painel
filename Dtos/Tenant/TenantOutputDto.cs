public class TenantOutputDto
{
    public Guid? Id { get; set; }
    public string? Nome { get; set; } = string.Empty;
    public string? NomeSecundario { get; set; }
    public string? Documento { get; set; } = string.Empty;
    public TipoTenant Tipo { get; set; }
    public string? Slug { get; set; } = string.Empty;

    // Campos de Auditoria (Herdados da sua AuditoriaModel através do item)
    public string Ativo { get; set; } = string.Empty;     
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; } = string.Empty;    
    public DateTime? Updated { get; set; }
    public string? UpdatedBy { get; set; }

    // Construtor vazio padrão
    public TenantOutputDto() {}

    // Construtor de mapeamento (Model -> DTO) baseado no modelo do Perfil
    public TenantOutputDto(TenantModel item) => 
        (Id, Nome, NomeSecundario, Documento, Tipo, Slug, Ativo, Created, CreatedBy, Updated, UpdatedBy) = 
        (item.Id, item.Nome, item.NomeSecundario, item.Documento, item.Tipo, item.Slug, item.Ativo, item.Created, item.CreatedBy, item.Updated, item.UpdatedBy);
}

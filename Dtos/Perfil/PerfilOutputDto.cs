
public class PerfilOutputDto
{
    public Guid? Id {get; set;}
    public string? Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    //Auditoria
    public string Ativo { get; set; }     
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }    
    public DateTime? Updated { get; set; }
    public string? UpdatedBy { get; set; }

    public PerfilOutputDto(){}
    // public PerfilOutputDto(PerfilModel item) => (Id, Nome, Descricao, Updated) = (item.Id, item.Nome, item.Descricao, item.Updated);
    public PerfilOutputDto(PerfilModel item) => (Id, Nome, Descricao, Ativo, Created, CreatedBy, Updated, UpdatedBy) = (item.Id, item.Nome, item.Descricao, item.Ativo, item.Created, item.CreatedBy, item.Updated, item.UpdatedBy);
    
}
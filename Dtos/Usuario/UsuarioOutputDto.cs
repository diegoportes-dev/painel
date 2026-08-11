public class UsuarioOutputDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public PerfilSummaryDto Perfil { get; set; } = null!;
        
    //Auditoria
    public string Ativo { get; set; }     
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }    
    public DateTime? Updated { get; set; }
    public string? UpdatedBy { get; set; }

    public UsuarioOutputDto() { }
    public UsuarioOutputDto(UsuarioModel item) => (Id, Email, Perfil, Ativo, Created, CreatedBy, Updated, UpdatedBy) = (item.Id, item.Email, item.Perfil is null ? null : new PerfilSummaryDto(item.Perfil), item.Ativo, item.Created, item.CreatedBy, item.Updated, item.UpdatedBy );
  
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
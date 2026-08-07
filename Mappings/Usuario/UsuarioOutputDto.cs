public class UsuarioOutputDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
        
    //Auditoria
    public string Ativo { get; set; }     
    public DateTime Created { get; set; }
    public string CreatedBy { get; set; }    
    public DateTime? Updated { get; set; }
    public string? UpdatedBy { get; set; }

    public Guid PerfilId { get; set; }
    public PerfilSummaryDto Perfil { get; set; } = null!;

    public UsuarioOutputDto() { }
    public UsuarioOutputDto(UsuarioModel item) => (Id, Email, SenhaHash, PerfilId, Perfil, Ativo, Created, CreatedBy, Updated, UpdatedBy) = (item.Id, item.Email, item.SenhaHash, item.PerfilId, item.Perfil is null ? null : new PerfilSummaryDto(item.Perfil), item.Ativo, item.Created, item.CreatedBy, item.Updated, item.UpdatedBy );
  
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
public abstract class AuditoriaModel
{
    // Campo ativo: "S" para Sim (Ativo), "N" para Não (Inativo)
    public string Ativo { get; set; } = "S"; 
    
    public DateTime Created { get; set; } = new DateTime(); // Inicializa com a data e hora atual
    public string CreatedBy { get; set; } = "System"; // Pode ser alterado pelo usuário logado
    
    public DateTime? Updated { get; set; }
    public string? UpdatedBy { get; set; }
}
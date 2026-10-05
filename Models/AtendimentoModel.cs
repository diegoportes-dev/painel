public class AtendimentoModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Prefixo { get; set; } = string.Empty;
    public int NumeroAtual { get; set; }
    public int GuicheAtual { get; set; }
    public string Prioridade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public DateTime? DataInicioAtendimento { get; set; }
    public DateTime? DataFinalizacaoAtendimento { get; set; }
    public string Observacao { get; set; } = string.Empty;

    public AtendimentoModel() : base() { }

    public AtendimentoModel(string prefixo, int numeroAtual, int guicheAtual, string prioridade, string status, string nomeCliente,
        DateTime? dataInicioAtendimento = null, DateTime? dataFinalizacaoAtendimento = null, string observacao = "")
        : base()
    {
        Id = Guid.NewGuid();
        Prefixo = prefixo;
        NumeroAtual = numeroAtual;
        GuicheAtual = guicheAtual;
        Prioridade = prioridade;
        Status = status;
        NomeCliente = nomeCliente;
        DataInicioAtendimento = dataInicioAtendimento ?? DateTime.UtcNow;
        DataFinalizacaoAtendimento = dataFinalizacaoAtendimento;
        Observacao = observacao;
    }
}

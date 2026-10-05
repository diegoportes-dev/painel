public class ChamadaSenhaDto
{
    public string Prefixo { get; set; } = string.Empty;
    public int NumeroAtual { get; set; }
    public int GuicheAtual { get; set; }
    public string Prioridade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
}

public class CriarAtendimentoDto
{
    public string Prefixo { get; set; } = string.Empty;
    public int NumeroAtual { get; set; }
    public int GuicheAtual { get; set; }
    public string Prioridade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string NomeCliente { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
}

public class StatusAtendimentoDto
{
    public string Prefixo { get; set; } = string.Empty;
    public int NumeroAtual { get; set; }
    public int GuicheAtual { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class FinalizarAtendimentoDto
{
    public string Prefixo { get; set; } = string.Empty;
    public int NumeroAtual { get; set; }
    public int GuicheAtual { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Observacao { get; set; } = string.Empty;
}

public class CancelarAtendimentoDto
{
    public string Prefixo { get; set; } = string.Empty;
    public int NumeroAtual { get; set; }
    public int GuicheAtual { get; set; }
    public string Observacao { get; set; } = string.Empty;
}

public class RotaModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string Rota {get; set;}
    public string Descricao {get; set;}
    public string Menu {get; set;}

    public RotaModel() : base() { }
}
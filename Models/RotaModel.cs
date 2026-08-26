public class RotaModel : AuditoriaModel
{
    public Guid Id { get; set; }
    public string EndPoint {get; set;}
    public string Rota {get; set;}
    public string Metodo {get; set;}
    public string Descricao {get; set;} //NomeEndPoint
    public string Menu {get; set;}

    public RotaModel() : base() { }
}
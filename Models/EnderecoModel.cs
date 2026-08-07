public class EnderecoModel
{
    public Guid Id {get; set;}
    public string Endereco {get; set;}
    public string? Numero {get; set;}
    public string? Bairro {get; set;}
    public string? Cidade {get; set;}
    public string? Uf {get; set;}
    public string? Cep  {get; set;}

    public Guid PersonId { get; set;}
    public PersonModel Person {get; set;} 
}
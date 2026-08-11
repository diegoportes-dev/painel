using System.ComponentModel.DataAnnotations;

public class EnderecoOutputDto
{    
    public Guid? Id {get; set;}
    public string Endereco {get; set;} = string.Empty;
    public string? Numero {get; set;}
    public string? Bairro {get; set;}
    public string? Cidade {get; set;}
    public string? Uf {get; set;}    
    public string? Cep  {get; set;}    
    public Guid PersonId {get; set;}
    public PersonSummaryDto? Person { get; set; }

    public EnderecoOutputDto(){}
    public EnderecoOutputDto(EnderecoModel item) => (Id, Endereco, Numero, Bairro, Cidade, Uf, Cep, PersonId, Person) = ( item.Id, item.Endereco, item.Numero, item.Bairro, item.Cidade, item.Uf, item.Cep, item.PersonId, item.Person is null ? null : new PersonSummaryDto(item.Person));
}

public class PersonSummaryDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }

    public PersonSummaryDto() { }
    public PersonSummaryDto(PersonModel person)
    {
        Id = person.Id;
        Name = person.Name;
    }
}

// public record EnderecoRequest(EnderecoModel endereco);
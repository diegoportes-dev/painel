// namespace Crud;

public class PersonModel
{
    public Guid Id { get; init; }
    public string? Name { get; private set; } = String.Empty;
    
    public PersonModel(string name)
    {
        Name = name;
        Id = Guid.NewGuid();
    }

    public void UpdatePerson(string? name)
    {
        Name = name;
    }
    
}
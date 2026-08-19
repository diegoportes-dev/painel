public interface ITenantProvider
{
    string? GetConnectionString();
    void SetConnectionString(string connectionString);
}

public class TenantProvider : ITenantProvider
{
    private string? _connectionString;

    public string? GetConnectionString() => _connectionString;
    public void SetConnectionString(string connectionString) => _connectionString = connectionString;
}

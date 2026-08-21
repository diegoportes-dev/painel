// using Microsoft.EntityFrameworkCore;

// public class TenantDbContext : DbContext
// {
//     public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

//     // Adicione aqui as tabelas operacionais que cada cliente terá de forma isolada:
//     // public DbSet<ProdutoModel> Produtos { get; set; }
//     // public DbSet<ClienteModel> Clientes { get; set; }
//     public DbSet<TesteModel> Testes {get; set;}

//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         base.OnModelCreating(modelBuilder);
//         // Configurações das suas tabelas de negócio...
//     }
// }



using Microsoft.EntityFrameworkCore;

public class TenantDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, ITenantProvider tenantProvider) 
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<TesteModel> Testes { get; set; }
    
    // // SQLLite
    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     // 1. Tenta recuperar a string de conexão dinâmica definida pelo Middleware (banco do cliente)
    //     var connectionString = _tenantProvider?.GetConnectionString();

    //     if (!string.IsNullOrEmpty(connectionString))
    //     {
    //         optionsBuilder.UseSqlite(connectionString);
    //     }
    //     // 2. Se não houver conexão dinâmica (ex: comandos dotnet ef no terminal ou fallback), usa o mestre
    //     else if (!optionsBuilder.IsConfigured)
    //     {
    //         optionsBuilder.UseSqlite("Data Source=crud.sqlite");
    //     }

    //     base.OnConfiguring(optionsBuilder);
    // }

    // MySQL
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {        
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

        var connectionString = _tenantProvider?.GetConnectionString();

        if (!string.IsNullOrEmpty(connectionString))
        {
            optionsBuilder.UseMySql(connectionString, serverVersion, x =>            
                x.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName)
            );
        }        
        else if (!optionsBuilder.IsConfigured)
        {
            string connectionStringFallback = "Server=localhost;Database=crud_central;Uid=root;Pwd=Teste123;";
            optionsBuilder.UseMySql(connectionStringFallback, serverVersion);
        }

        base.OnConfiguring(optionsBuilder);
    }
}


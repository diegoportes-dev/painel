using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Crud.Data;

public class CrudContext() : DbContext
{
    public DbSet<UsuarioModel>Usuarios{ get; set; }
    public DbSet<PerfilModel>Perfis{ get; set; }
    public DbSet<TenantModel>Tenants{ get; set;}
    public DbSet<RotaModel>Rotas{get; set;}
    public DbSet<NivelAcessoModel>NiveisAcessos{get; set;}
    public DbSet<AtendimentoModel>Atendimentos{get; set;}


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        // Dentro do OnModelCreating, se quiser Guids mais eficientes em formato binário (opcional)
        modelBuilder.Entity<TenantModel>().Property(t => t.Id).HasColumnType("char(36)");

        // Configura o relacionamento para bloquear a exclusão em cascata
        modelBuilder.Entity<UsuarioModel>()
            .HasOne(u => u.Perfil)
            .WithMany()
            .HasForeignKey(u => u.PerfilId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UsuarioModel>()
            .HasOne(u => u.Tenant)
            .WithMany() 
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PerfilModel>()
            .HasOne(p => p.Tenant)
            .WithMany() 
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NivelAcessoModel>()
            .HasOne(p => p.Tenant)
            .WithMany() 
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NivelAcessoModel>()
            .HasOne(u => u.Perfil)
            .WithMany()
            .HasForeignKey(u => u.PefilId)
            .OnDelete(DeleteBehavior.Restrict);

    }

    // // SQLLite
    // protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    // {
    //     // optionsBuilder.UseSqlite(connectionString: "Data Source=crud.sqlite");

    //     // Só usa o arquivo físico se o DbContext não tiver sido configurado por fora (via Injeção de Dependência)
    //     if (!optionsBuilder.IsConfigured)
    //     {
    //         optionsBuilder.UseSqlite("Data Source=crud.sqlite");
    //     }
    //     base.OnConfiguring(optionsBuilder);
    // }

    // MySQL
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Leitura dinâmica do appsettings.json no terminal
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            string connectionString = configuration.GetConnectionString("CentralConnection");

            // 2. Defina a versão do seu servidor MySQL ou MariaDB (Exemplo: Versão 8.0 do MySQL)
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 0));

            // 3. Configure o provedor Pomelo
            optionsBuilder.UseMySql(connectionString, serverVersion);
        }
        base.OnConfiguring(optionsBuilder);
    }


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Captura todas as entidades que herdam de EntidadeAuditoria
        var entradas = ChangeTracker.Entries()
            .Where(e => e.Entity is AuditoriaModel && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entrada in entradas)
        {
            var entidade = (AuditoriaModel)entrada.Entity;
            var dataAtual = DateTime.UtcNow;

            if (entrada.State == EntityState.Added)
            {
                entidade.Created = dataAtual;
                entidade.Ativo = "S"; // Garante que nasce ativo
                entidade.CreatedBy = "UsuarioLogado"; // Caso mude no futuro
            }
            else if (entrada.State == EntityState.Modified)
            {
                entidade.Updated = dataAtual;
                entidade.UpdatedBy = "UsuarioLogado"; // Caso mude no futuro
                // impede que o EF limpe ou altere a data de criação original no update
                entrada.Property(nameof(AuditoriaModel.Created)).IsModified = false;
                entrada.Property(nameof(AuditoriaModel.CreatedBy)).IsModified = false;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
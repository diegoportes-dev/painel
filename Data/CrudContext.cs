using Microsoft.EntityFrameworkCore;

namespace Crud.Data;

public class CrudContext() : DbContext
{
    public DbSet<PersonModel>People{ get; set; }
    public DbSet<EnderecoModel>Endereco{ get; set; } 
    public DbSet<UsuarioModel>Usuarios{ get; set; }
    public DbSet<PerfilModel>Perfis{ get; set; }

    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     modelBuilder.Entity<UsuarioModel>().ToTable("Usuario");
    //     modelBuilder.Entity<PerfilModel>().ToTable("Perfil");

    //     base.OnModelCreating(modelBuilder);
    // }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(connectionString: "Data Source=crud.sqlite");
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
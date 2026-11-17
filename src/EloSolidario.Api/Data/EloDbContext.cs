using EloSolidario.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EloSolidario.Api.Data;

public class EloDbContext : DbContext
{
    public EloDbContext(DbContextOptions<EloDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<ItemDoacao> ItensDoacao => Set<ItemDoacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var simNao = new ValueConverter<bool, string>(valor => valor ? "S" : "N", valor => valor == "S");

        modelBuilder.Entity<Usuario>(entidade =>
        {
            entidade.ToTable("TB_ELO_USUARIO");
            entidade.HasKey(u => u.Id);
            entidade.Property(u => u.Id).HasColumnName("ID_USUARIO").ValueGeneratedOnAdd();
            entidade.Property(u => u.Nome).HasColumnName("NOME").HasMaxLength(100);
            entidade.Property(u => u.Email).HasColumnName("EMAIL").HasMaxLength(120);
            entidade.Property(u => u.Senha).HasColumnName("SENHA").HasMaxLength(100);
            entidade.Property(u => u.Perfil).HasColumnName("PERFIL").HasMaxLength(15);
            entidade.Property(u => u.Bairro).HasColumnName("BAIRRO").HasMaxLength(60);
        });

        modelBuilder.Entity<Categoria>(entidade =>
        {
            entidade.ToTable("TB_ELO_CATEGORIA");
            entidade.HasKey(c => c.Id);
            entidade.Property(c => c.Id).HasColumnName("ID_CATEGORIA").ValueGeneratedOnAdd();
            entidade.Property(c => c.Nome).HasColumnName("NOME").HasMaxLength(60);
            entidade.Property(c => c.Perecivel).HasColumnName("PERECIVEL").HasConversion(simNao).HasMaxLength(1);
        });

        modelBuilder.Entity<Alimento>(entidade =>
        {
            entidade.ToTable("TB_ELO_ALIMENTO");
            entidade.HasKey(a => a.Id);
            entidade.Property(a => a.Id).HasColumnName("ID_ALIMENTO").ValueGeneratedOnAdd();
            entidade.Property(a => a.IdCategoria).HasColumnName("ID_CATEGORIA");
            entidade.Property(a => a.Nome).HasColumnName("NOME").HasMaxLength(80);
            entidade.Property(a => a.Conservacao).HasColumnName("CONSERVACAO").HasConversion<string>().HasMaxLength(12);
            entidade.Property(a => a.Calorias100g).HasColumnName("CALORIAS_100G").HasPrecision(6, 1);
            entidade.Property(a => a.Ativo).HasColumnName("ATIVO").HasConversion(simNao).HasMaxLength(1);
            entidade.HasOne(a => a.Categoria)
                .WithMany(c => c.Alimentos)
                .HasForeignKey(a => a.IdCategoria);
        });

        modelBuilder.Entity<ItemDoacao>(entidade =>
        {
            entidade.ToTable("TB_ELO_ITEM_DOACAO");
            entidade.HasKey(i => i.Id);
            entidade.Property(i => i.Id).HasColumnName("ID_ITEM");
            entidade.Property(i => i.IdDoacao).HasColumnName("ID_DOACAO");
            entidade.Property(i => i.IdAlimento).HasColumnName("ID_ALIMENTO");
        });
    }
}

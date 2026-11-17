using EloSolidario.Api.Data;
using EloSolidario.Api.Domain;

namespace EloSolidario.Tests.Apoio;

public static class DadosTeste
{
    public const string Senha = "Elo@2026";

    public static void Popular(EloDbContext db)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Senha, 4);
        db.Usuarios.AddRange(
            new Usuario { Id = 1, Nome = "Ana Souza", Email = "ana@email.com", Senha = hash, Perfil = Perfil.Doador, Bairro = "Mooca" },
            new Usuario { Id = 2, Nome = "Maria Oliveira", Email = "maria@email.com", Senha = hash, Perfil = Perfil.Beneficiario, Bairro = "Tatuapé" },
            new Usuario { Id = 3, Nome = "Admin", Email = "admin@email.com", Senha = hash, Perfil = Perfil.Admin, Bairro = "Centro" });

        var graos = new Categoria { Id = 1, Nome = "Grãos e Cereais", Perecivel = false };
        var laticinios = new Categoria { Id = 2, Nome = "Laticínios", Perecivel = true };
        db.Categorias.AddRange(graos, laticinios);

        db.Alimentos.AddRange(
            new Alimento { Id = 1, IdCategoria = 1, Nome = "Arroz branco", Conservacao = Conservacao.AMBIENTE, Calorias100g = 358, Ativo = true },
            new Alimento { Id = 2, IdCategoria = 1, Nome = "Feijão carioca", Conservacao = Conservacao.AMBIENTE, Calorias100g = 329, Ativo = true },
            new Alimento { Id = 3, IdCategoria = 2, Nome = "Iogurte natural", Conservacao = Conservacao.REFRIGERADO, Calorias100g = 51, Ativo = true },
            new Alimento { Id = 4, IdCategoria = 2, Nome = "Queijo minas", Conservacao = Conservacao.REFRIGERADO, Calorias100g = 264, Ativo = false });

        db.ItensDoacao.Add(new ItemDoacao { Id = 1, IdDoacao = 1, IdAlimento = 1 });
        db.SaveChanges();
    }
}

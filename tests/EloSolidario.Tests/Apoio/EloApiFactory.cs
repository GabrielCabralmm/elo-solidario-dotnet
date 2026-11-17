using EloSolidario.Api.Data;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EloSolidario.Tests.Apoio;

public class EloApiFactory : WebApplicationFactory<Program>
{
    public const string Segredo = "segredo-de-teste-com-mais-de-32-caracteres!";

    private readonly string _nomeBanco = Guid.NewGuid().ToString();

    public int LimiteLogin { get; init; } = 100;

    public ImpactoRepositoryFalso Impacto { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Oracle", "Data Source=nao-usado");
        builder.UseSetting("Jwt:Secret", Segredo);
        builder.UseSetting("ApiJava:Url", "");
        builder.UseSetting("LimiteRequisicoes:LoginPorMinuto", LimiteLogin.ToString());
        builder.UseSetting("LimiteRequisicoes:GeralPorMinuto", "1000");
        builder.UseSetting("LimiteRequisicoes:EscritaPorMinuto", "1000");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EloDbContext>>();
            services.AddDbContext<EloDbContext>(opcoes => opcoes.UseInMemoryDatabase(_nomeBanco));

            services.RemoveAll<IImpactoRepository>();
            services.AddSingleton<IImpactoRepository>(Impacto);

            using var escopo = services.BuildServiceProvider().CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<EloDbContext>();
            db.Database.EnsureCreated();
            DadosTeste.Popular(db);
        });
    }

    public HttpClient ClienteComo(string perfil)
    {
        var usuario = perfil switch
        {
            Perfil.Admin => new Usuario { Id = 3, Email = "admin@email.com", Perfil = Perfil.Admin },
            Perfil.Doador => new Usuario { Id = 1, Email = "ana@email.com", Perfil = Perfil.Doador },
            _ => new Usuario { Id = 2, Email = "maria@email.com", Perfil = Perfil.Beneficiario }
        };

        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = Segredo })
            .Build();
        var token = new TokenService(configuracao).Gerar(usuario);

        var cliente = CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

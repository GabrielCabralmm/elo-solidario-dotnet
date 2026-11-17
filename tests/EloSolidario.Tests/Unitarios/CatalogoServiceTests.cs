using EloSolidario.Api.Data;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Api.Infra.Erros;
using EloSolidario.Api.Services;
using EloSolidario.Tests.Apoio;
using Microsoft.EntityFrameworkCore;

namespace EloSolidario.Tests.Unitarios;

public class CatalogoServiceTests
{
    private readonly EloDbContext _db;
    private readonly CatalogoService _service;

    public CatalogoServiceTests()
    {
        var opcoes = new DbContextOptionsBuilder<EloDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new EloDbContext(opcoes);
        DadosTeste.Popular(_db);
        _service = new CatalogoService(_db);
    }

    [Fact]
    public async Task CriarAlimento_ComDadosValidos_DeveGravarAtivo()
    {
        var resultado = await _service.CriarAlimentoAsync(Requisicao("Lentilha", idCategoria: 1));

        Assert.True(resultado.Sucesso);
        Assert.True(resultado.Valor!.Ativo);
        Assert.Equal("Grãos e Cereais", resultado.Valor.Categoria);
        Assert.Equal(5, await _db.Alimentos.CountAsync());
    }

    [Fact]
    public async Task CriarAlimento_ComNomeJaExistente_DeveRetornarConflitoIgnorandoMaiusculas()
    {
        var resultado = await _service.CriarAlimentoAsync(Requisicao("ARROZ BRANCO", idCategoria: 1));

        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Equal(4, await _db.Alimentos.CountAsync());
    }

    [Fact]
    public async Task CriarAlimento_ComCategoriaInexistente_DeveRetornarNaoEncontrado()
    {
        var resultado = await _service.CriarAlimentoAsync(Requisicao("Lentilha", idCategoria: 99));

        Assert.Equal(TipoErro.NaoEncontrado, resultado.Erro);
    }

    [Fact]
    public async Task ExcluirAlimento_UsadoEmDoacao_DeveRetornarConflitoESugerirDesativar()
    {
        var resultado = await _service.ExcluirAlimentoAsync(1);

        Assert.Equal(TipoErro.Conflito, resultado.Erro);
        Assert.Contains("Desative", resultado.Mensagem);
        Assert.NotNull(await _db.Alimentos.FindAsync(1L));
    }

    [Fact]
    public async Task ListarAlimentos_SemIncluirInativos_NaoDeveTrazerAlimentoDesativado()
    {
        var pagina = await _service.ListarAlimentosAsync(null, null, null, incluirInativos: false, pagina: 1, tamanho: 10);

        Assert.Equal(3, pagina.TotalItens);
        Assert.DoesNotContain(pagina.Itens, a => a.Nome == "Queijo minas");
    }

    [Fact]
    public async Task ListarAlimentos_DevePaginarECalcularTotalDePaginas()
    {
        var pagina = await _service.ListarAlimentosAsync(null, null, null, incluirInativos: true, pagina: 2, tamanho: 3);

        Assert.Single(pagina.Itens);
        Assert.Equal(4, pagina.TotalItens);
        Assert.Equal(2, pagina.TotalPaginas);
    }

    [Theory]
    [InlineData(Conservacao.AMBIENTE, "seco")]
    [InlineData(Conservacao.REFRIGERADO, "5 °C")]
    [InlineData(Conservacao.CONGELADO, "-18 °C")]
    public void OrientacaoPara_DeveVariarConformeAConservacao(Conservacao conservacao, string trechoEsperado)
    {
        Assert.Contains(trechoEsperado, CatalogoService.OrientacaoPara(conservacao));
    }

    private static AlimentoRequest Requisicao(string nome, long idCategoria) => new()
    {
        Nome = nome,
        IdCategoria = idCategoria,
        Conservacao = Conservacao.AMBIENTE,
        Calorias100g = 116
    };
}

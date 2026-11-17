using System.Net;
using System.Net.Http.Json;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Tests.Apoio;

namespace EloSolidario.Tests.Integracao;

public class AlimentoEndpointsTests : IClassFixture<EloApiFactory>
{
    private readonly EloApiFactory _factory;

    public AlimentoEndpointsTests(EloApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Listar_DeveRetornarPaginaSomenteComAlimentosAtivos()
    {
        var pagina = await _factory.ClienteComo(Perfil.Beneficiario)
            .GetFromJsonAsync<PaginaResponse<AlimentoResponse>>("/alimentos?idCategoria=2&tamanho=5");

        Assert.Single(pagina!.Itens);
        Assert.Equal("Iogurte natural", pagina.Itens[0].Nome);
        Assert.Contains("5 °C", pagina.Itens[0].OrientacaoConservacao);
    }

    [Fact]
    public async Task Listar_ComTamanhoForaDoLimite_DeveRetornar400()
    {
        var resposta = await _factory.ClienteComo(Perfil.Beneficiario).GetAsync("/alimentos?tamanho=100");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Buscar_AlimentoInexistente_DeveRetornar404()
    {
        var resposta = await _factory.ClienteComo(Perfil.Beneficiario).GetAsync("/alimentos/999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Criar_ComDadosInvalidos_DeveRetornar400ComErrosPorCampo()
    {
        var cliente = ClienteAdminComChave(Guid.NewGuid().ToString());

        var resposta = await cliente.PostAsJsonAsync("/alimentos", new { idCategoria = 1, nome = "", calorias100g = 5000 });
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("nome", corpo);
        Assert.Contains("conservacao", corpo);
        Assert.Contains("calorias100g", corpo);
    }

    [Fact]
    public async Task Criar_ComValorDeEnumInexistente_DeveRetornar400()
    {
        var cliente = ClienteAdminComChave(Guid.NewGuid().ToString());

        var resposta = await cliente.PostAsJsonAsync("/alimentos",
            new { idCategoria = 1, nome = "Teste", conservacao = "QUENTE", calorias100g = 10 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task CicloCompleto_CriarAtualizarDesativarEExcluir()
    {
        var cliente = ClienteAdminComChave(Guid.NewGuid().ToString());
        var criado = await cliente.PostAsJsonAsync("/alimentos",
            new { idCategoria = 1, nome = "Grão-de-bico", conservacao = "AMBIENTE", calorias100g = 364 });
        var alimento = await criado.Content.ReadFromJsonAsync<AlimentoResponse>();

        var atualizado = await cliente.PutAsJsonAsync($"/alimentos/{alimento!.Id}",
            new { idCategoria = 1, nome = "Grão-de-bico", conservacao = "AMBIENTE", calorias100g = 360 });
        var desativado = await cliente.PatchAsJsonAsync($"/alimentos/{alimento.Id}/ativo", new { ativo = false });
        var excluido = await cliente.DeleteAsync($"/alimentos/{alimento.Id}");
        var buscado = await cliente.GetAsync($"/alimentos/{alimento.Id}");

        Assert.Equal(HttpStatusCode.Created, criado.StatusCode);
        Assert.Equal($"/alimentos/{alimento.Id}", criado.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, atualizado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, desativado.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, excluido.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, buscado.StatusCode);
    }

    [Fact]
    public async Task Excluir_AlimentoUsadoEmDoacao_DeveRetornar409()
    {
        var resposta = await _factory.ClienteComo(Perfil.Admin).DeleteAsync("/alimentos/1");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    private HttpClient ClienteAdminComChave(string chave)
    {
        var cliente = _factory.ClienteComo(Perfil.Admin);
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", chave);
        return cliente;
    }
}

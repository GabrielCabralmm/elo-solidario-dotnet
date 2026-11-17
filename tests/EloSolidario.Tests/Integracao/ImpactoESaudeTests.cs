using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Tests.Apoio;

namespace EloSolidario.Tests.Integracao;

public class ImpactoESaudeTests : IClassFixture<EloApiFactory>
{
    private readonly EloApiFactory _factory;

    public ImpactoESaudeTests(EloApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Impacto_DeveRetornarOPainelParaQualquerUsuarioLogado()
    {
        var impacto = await _factory.ClienteComo(Perfil.Beneficiario).GetFromJsonAsync<ImpactoResponse>("/impacto");

        Assert.Equal(27.25m, impacto!.AlimentosDestinadosKg);
        Assert.Equal(100m, impacto.RefeicoesEstimadas);
    }

    [Fact]
    public async Task ImpactoDoDoador_Inexistente_DeveRetornar404()
    {
        var resposta = await _factory.ClienteComo(Perfil.Beneficiario).GetAsync("/impacto/doadores/2");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Urgentes_ComDiasForaDoIntervalo_DeveRetornar400()
    {
        var resposta = await _factory.ClienteComo(Perfil.Beneficiario).GetAsync("/doacoes/urgentes?dias=31");

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Urgentes_DeveTrazerOAlertaDeValidade()
    {
        var urgentes = await _factory.ClienteComo(Perfil.Beneficiario)
            .GetFromJsonAsync<List<DoacaoUrgenteResponse>>("/doacoes/urgentes?dias=3");

        Assert.Equal("VENCE AMANHÃ", urgentes!.Single().Alerta);
    }

    [Fact]
    public async Task Health_DeveSerPublicoEListarApiBancoEApiJava()
    {
        var resposta = await _factory.CreateClient().GetAsync("/health");
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var nomes = json.RootElement.GetProperty("verificacoes").EnumerateArray()
            .Select(v => v.GetProperty("nome").GetString()).ToList();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(new[] { "api", "oracle", "api-java" }, nomes);
        Assert.Equal("Degraded", json.RootElement.GetProperty("status").GetString());
    }
}

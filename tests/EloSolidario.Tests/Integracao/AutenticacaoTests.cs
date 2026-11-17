using System.Net;
using System.Net.Http.Json;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Tests.Apoio;

namespace EloSolidario.Tests.Integracao;

public class AutenticacaoTests : IClassFixture<EloApiFactory>
{
    private readonly EloApiFactory _factory;

    public AutenticacaoTests(EloApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EndpointProtegido_SemToken_DeveRetornar401()
    {
        var resposta = await _factory.CreateClient().GetAsync("/alimentos");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_ComCredenciaisValidas_DeveRetornarTokenQueDaAcessoAApi()
    {
        var cliente = _factory.CreateClient();
        var login = await cliente.PostAsJsonAsync("/auth/login", new { email = "maria@email.com", senha = DadosTeste.Senha });
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();

        cliente.DefaultRequestHeaders.Authorization = new("Bearer", token!.Token);
        var me = await cliente.GetFromJsonAsync<UsuarioResponse>("/auth/me");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Equal(Perfil.Beneficiario, me!.Perfil);
    }

    [Fact]
    public async Task Login_ComSenhaErrada_DeveRetornar401()
    {
        var resposta = await _factory.CreateClient().PostAsJsonAsync("/auth/login", new { email = "maria@email.com", senha = "errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_ComCorpoInvalido_DeveRetornar400ComOsCampos()
    {
        var resposta = await _factory.CreateClient().PostAsJsonAsync("/auth/login", new { email = "invalido", senha = "" });
        var corpo = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("email", corpo);
        Assert.Contains("senha", corpo);
    }

    [Fact]
    public async Task EndpointDeAdmin_ComPerfilBeneficiario_DeveRetornar403()
    {
        var cliente = _factory.ClienteComo(Perfil.Beneficiario);
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var resposta = await cliente.PostAsJsonAsync("/categorias", new { nome = "Bebidas", perecivel = false });

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Tests.Apoio;

namespace EloSolidario.Tests.Integracao;

public class IdempotenciaTests : IClassFixture<EloApiFactory>
{
    private readonly EloApiFactory _factory;

    public IdempotenciaTests(EloApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_SemIdempotencyKey_DeveRetornar400()
    {
        var resposta = await _factory.ClienteComo(Perfil.Admin)
            .PostAsJsonAsync("/categorias", new { nome = "Congelados", perecivel = true });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Post_RepetidoComMesmaChave_DeveDevolverAMesmaRespostaSemDuplicar()
    {
        var cliente = _factory.ClienteComo(Perfil.Admin);
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var corpo = new { idCategoria = 2, nome = "Requeijão", conservacao = "REFRIGERADO", calorias100g = 257 };

        var primeira = await cliente.PostAsJsonAsync("/alimentos", corpo);
        var segunda = await cliente.PostAsJsonAsync("/alimentos", corpo);
        var alimento1 = await primeira.Content.ReadFromJsonAsync<AlimentoResponse>();
        var alimento2 = await segunda.Content.ReadFromJsonAsync<AlimentoResponse>();
        var busca = await cliente.GetFromJsonAsync<PaginaResponse<AlimentoResponse>>("/alimentos?nome=requeij");

        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(alimento1!.Id, alimento2!.Id);
        Assert.False(primeira.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal("true", segunda.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(primeira.Headers.Location, segunda.Headers.Location);
        Assert.Equal(1, busca!.TotalItens);
    }

    [Fact]
    public async Task Post_MesmaChaveComOutroCorpo_DeveRetornar422()
    {
        var cliente = _factory.ClienteComo(Perfil.Admin);
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        await cliente.PostAsJsonAsync("/categorias", new { nome = "Bebidas", perecivel = false });
        var segunda = await cliente.PostAsJsonAsync("/categorias", new { nome = "Doces", perecivel = false });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
    }

    [Fact]
    public async Task Post_QueFalhou_NaoDeveSerGuardadoEPodeSerCorrigidoComAMesmaChave()
    {
        var cliente = _factory.ClienteComo(Perfil.Admin);
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var comErro = await cliente.PostAsJsonAsync("/alimentos",
            new { idCategoria = 99, nome = "Pepino", conservacao = "REFRIGERADO", calorias100g = 15 });
        var corrigido = await cliente.PostAsJsonAsync("/alimentos",
            new { idCategoria = 99, nome = "Pepino", conservacao = "REFRIGERADO", calorias100g = 15 });

        Assert.Equal(HttpStatusCode.NotFound, comErro.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, corrigido.StatusCode);
    }

    [Fact]
    public async Task ExpirarDoacoes_RepetidoComMesmaChave_DeveExecutarAProcedureUmaUnicaVez()
    {
        var factory = new EloApiFactory();
        var cliente = factory.ClienteComo(Perfil.Admin);
        cliente.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        var primeira = await cliente.PostAsync("/admin/doacoes/expirar", null);
        var segunda = await cliente.PostAsync("/admin/doacoes/expirar", null);

        Assert.Equal(HttpStatusCode.OK, primeira.StatusCode);
        Assert.Equal(await primeira.Content.ReadAsStringAsync(), await segunda.Content.ReadAsStringAsync());
        Assert.Equal(1, factory.Impacto.ExecucoesExpiracao);
    }
}

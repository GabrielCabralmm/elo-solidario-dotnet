using System.Net;
using System.Net.Http.Json;
using EloSolidario.Tests.Apoio;

namespace EloSolidario.Tests.Integracao;

public class LimiteRequisicoesTests
{
    [Fact]
    public async Task Login_AcimaDoLimite_DeveRetornar429ComRetryAfter()
    {
        await using var factory = new EloApiFactory { LimiteLogin = 3 };
        var cliente = factory.CreateClient();
        var corpo = new { email = "maria@email.com", senha = "errada" };

        var codigos = new List<HttpStatusCode>();
        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 4; i++)
        {
            ultima = await cliente.PostAsJsonAsync("/auth/login", corpo);
            codigos.Add(ultima.StatusCode);
        }

        Assert.Equal(new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests }, codigos);
        Assert.True(ultima!.Headers.Contains("Retry-After"));
        Assert.Contains("Limite de requisições excedido", await ultima.Content.ReadAsStringAsync());
    }
}

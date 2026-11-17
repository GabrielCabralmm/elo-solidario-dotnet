using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EloSolidario.Api.Infra.Saude;

public class ApiJavaHealthCheck : IHealthCheck
{
    public const string NomeCliente = "api-java";

    private readonly IHttpClientFactory _httpClientFactory;

    public ApiJavaHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cliente = _httpClientFactory.CreateClient(NomeCliente);
        if (cliente.BaseAddress is null)
        {
            return HealthCheckResult.Degraded("URL da API Java não configurada (ApiJava:Url)");
        }

        var cronometro = Stopwatch.StartNew();
        try
        {
            using var resposta = await cliente.GetAsync("status", cancellationToken);
            var dados = new Dictionary<string, object>
            {
                ["url"] = cliente.BaseAddress.ToString(),
                ["statusHttp"] = (int)resposta.StatusCode,
                ["tempoRespostaMs"] = cronometro.ElapsedMilliseconds
            };

            return resposta.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("API Java (doações e reservas) respondendo", dados)
                : HealthCheckResult.Degraded("API Java respondeu com erro", data: dados);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("API Java indisponível. O catálogo e o impacto continuam funcionando", ex);
        }
    }
}

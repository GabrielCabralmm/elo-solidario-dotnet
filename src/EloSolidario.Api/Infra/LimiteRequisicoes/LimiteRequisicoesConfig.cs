using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EloSolidario.Api.Infra.LimiteRequisicoes;

public static class LimiteRequisicoesConfig
{
    public const string PoliticaLogin = "login";
    public const string PoliticaEscrita = "escrita";

    public static IServiceCollection AddLimiteRequisicoes(this IServiceCollection services, IConfiguration configuration)
    {
        var limiteGeral = configuration.GetValue("LimiteRequisicoes:GeralPorMinuto", 100);
        var limiteLogin = configuration.GetValue("LimiteRequisicoes:LoginPorMinuto", 5);
        var limiteEscrita = configuration.GetValue("LimiteRequisicoes:EscritaPorMinuto", 20);

        return services.AddRateLimiter(opcoes =>
        {
            opcoes.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                RateLimitPartition.GetFixedWindowLimiter(Identificar(http), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limiteGeral,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            opcoes.AddPolicy(PoliticaLogin, http =>
                RateLimitPartition.GetFixedWindowLimiter($"login:{IpDe(http)}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limiteLogin,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            opcoes.AddPolicy(PoliticaEscrita, http =>
                RateLimitPartition.GetSlidingWindowLimiter($"escrita:{Identificar(http)}", _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = limiteEscrita,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0
                }));

            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opcoes.OnRejected = async (contexto, cancelamento) =>
            {
                var segundos = 60;
                if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var espera))
                {
                    segundos = (int)Math.Ceiling(espera.TotalSeconds);
                }

                contexto.HttpContext.Response.Headers.RetryAfter = segundos.ToString(CultureInfo.InvariantCulture);
                await Results.Problem(
                        $"Limite de requisições excedido. Tente novamente em {segundos} segundos.",
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Muitas requisições")
                    .ExecuteAsync(contexto.HttpContext);
            };
        });
    }

    private static string Identificar(HttpContext http)
    {
        var usuario = http.User.FindFirst("sub")?.Value;
        return usuario is not null ? $"usuario:{usuario}" : $"ip:{IpDe(http)}";
    }

    private static string IpDe(HttpContext http)
    {
        var encaminhado = http.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(encaminhado))
        {
            return encaminhado.Split(',')[0].Trim();
        }
        return http.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    }
}

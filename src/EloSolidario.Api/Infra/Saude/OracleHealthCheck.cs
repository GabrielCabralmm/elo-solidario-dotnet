using System.Diagnostics;
using EloSolidario.Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EloSolidario.Api.Infra.Saude;

public class OracleHealthCheck : IHealthCheck
{
    private readonly EloDbContext _db;

    public OracleHealthCheck(EloDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            var conectou = await _db.Database.CanConnectAsync(cancellationToken);
            var dados = new Dictionary<string, object> { ["tempoRespostaMs"] = cronometro.ElapsedMilliseconds };

            return conectou
                ? HealthCheckResult.Healthy("Conexão com o Oracle disponível", dados)
                : HealthCheckResult.Unhealthy("Não foi possível conectar ao Oracle", data: dados);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Erro ao conectar ao Oracle", ex);
        }
    }
}

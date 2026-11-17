using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EloSolidario.Api.Infra.Saude;

public static class RespostaSaude
{
    public static Task EscreverAsync(HttpContext context, HealthReport relatorio)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var corpo = new
        {
            status = relatorio.Status.ToString(),
            duracaoMs = Math.Round(relatorio.TotalDuration.TotalMilliseconds, 1),
            verificacoes = relatorio.Entries.Select(e => new
            {
                nome = e.Key,
                status = e.Value.Status.ToString(),
                descricao = e.Value.Description,
                duracaoMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                erro = e.Value.Exception?.Message,
                dados = e.Value.Data
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(corpo, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
    }
}

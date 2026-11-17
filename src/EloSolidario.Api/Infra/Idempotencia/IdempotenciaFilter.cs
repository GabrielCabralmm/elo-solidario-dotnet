using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace EloSolidario.Api.Infra.Idempotencia;

public class IdempotenciaFilter : IEndpointFilter
{
    public const string Cabecalho = "Idempotency-Key";
    public const string CabecalhoReplay = "Idempotent-Replayed";

    private static readonly TimeSpan Validade = TimeSpan.FromHours(24);
    private static readonly TimeSpan LimiteProcessamento = TimeSpan.FromSeconds(30);

    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _json;

    public IdempotenciaFilter(IDistributedCache cache, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions)
    {
        _cache = cache;
        _json = jsonOptions.Value.SerializerOptions;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var chave = http.Request.Headers[Cabecalho].ToString();

        if (string.IsNullOrWhiteSpace(chave) || chave.Length > 100)
        {
            return Results.Problem($"Envie o cabeçalho {Cabecalho} com um identificador único (até 100 caracteres) para esta operação",
                statusCode: StatusCodes.Status400BadRequest, title: "Chave de idempotência ausente");
        }

        var usuario = http.User.FindFirst("sub")?.Value ?? "anonimo";
        var chaveCache = $"idempotencia:{usuario}:{http.Request.Method}:{http.Request.Path}:{chave}";
        var hash = CalcularHash(context.Arguments);

        var existente = await LerAsync(chaveCache);
        if (existente is not null)
        {
            if (existente.HashRequisicao != hash)
            {
                return Results.Problem("Esta Idempotency-Key já foi usada com um conteúdo diferente. Gere uma nova chave para uma nova operação",
                    statusCode: StatusCodes.Status422UnprocessableEntity, title: "Chave de idempotência reutilizada");
            }
            if (existente.EmProcessamento)
            {
                return Results.Problem("Uma requisição com esta Idempotency-Key ainda está em processamento",
                    statusCode: StatusCodes.Status409Conflict, title: "Requisição duplicada em andamento");
            }

            http.Response.Headers[CabecalhoReplay] = "true";
            if (existente.Location is not null)
            {
                http.Response.Headers.Location = existente.Location;
            }
            return Results.Content(existente.CorpoJson ?? string.Empty, "application/json", Encoding.UTF8, existente.StatusCode);
        }

        await GravarAsync(chaveCache, new RegistroIdempotencia(hash, true, 0, null, null), LimiteProcessamento);

        object? resultado;
        try
        {
            resultado = await next(context);
        }
        catch
        {
            await _cache.RemoveAsync(chaveCache);
            throw;
        }

        var statusCode = (resultado as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;
        if (statusCode is >= 200 and < 300)
        {
            var valor = (resultado as IValueHttpResult)?.Value;
            var location = resultado?.GetType().GetProperty("Location")?.GetValue(resultado) as string;
            var corpo = valor is null ? null : JsonSerializer.Serialize(valor, valor.GetType(), _json);
            await GravarAsync(chaveCache, new RegistroIdempotencia(hash, false, statusCode, corpo, location), Validade);
        }
        else
        {
            await _cache.RemoveAsync(chaveCache);
        }

        return resultado;
    }

    private string CalcularHash(IList<object?> argumentos)
    {
        var dados = argumentos
            .Where(a => a is not null && (a.GetType().Namespace?.StartsWith("EloSolidario.Api.Dtos") == true || a.GetType().IsPrimitive))
            .Select(a => JsonSerializer.Serialize(a, a!.GetType(), _json));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", dados)));
        return Convert.ToHexString(bytes);
    }

    private async Task<RegistroIdempotencia?> LerAsync(string chave)
    {
        var bytes = await _cache.GetAsync(chave);
        return bytes is null ? null : JsonSerializer.Deserialize<RegistroIdempotencia>(bytes);
    }

    private Task GravarAsync(string chave, RegistroIdempotencia registro, TimeSpan validade)
    {
        return _cache.SetAsync(chave, JsonSerializer.SerializeToUtf8Bytes(registro),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = validade });
    }
}

public static class IdempotenciaExtensions
{
    public static RouteHandlerBuilder ComIdempotencia(this RouteHandlerBuilder builder)
    {
        return builder
            .AddEndpointFilter<IdempotenciaFilter>()
            .WithOpenApi(operacao =>
            {
                operacao.Parameters.Add(new Microsoft.OpenApi.Models.OpenApiParameter
                {
                    Name = IdempotenciaFilter.Cabecalho,
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Required = true,
                    Description = "Identificador único da operação (ex.: um GUID). Repetir a mesma chave devolve a resposta original sem gravar de novo.",
                    Schema = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "uuid" }
                });
                return operacao;
            })
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}

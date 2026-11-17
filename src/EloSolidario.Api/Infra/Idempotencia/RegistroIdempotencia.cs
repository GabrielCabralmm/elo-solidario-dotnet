namespace EloSolidario.Api.Infra.Idempotencia;

public record RegistroIdempotencia(string HashRequisicao, bool EmProcessamento, int StatusCode, string? CorpoJson,
    string? Location);

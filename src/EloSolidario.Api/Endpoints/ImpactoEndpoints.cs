using EloSolidario.Api.Dtos;
using EloSolidario.Api.Infra.Idempotencia;
using EloSolidario.Api.Infra.LimiteRequisicoes;
using EloSolidario.Api.Services;

namespace EloSolidario.Api.Endpoints;

public static class ImpactoEndpoints
{
    public static void MapImpactoEndpoints(this WebApplication app)
    {
        var impacto = app.MapGroup("/impacto").WithTags("Impacto social").RequireAuthorization();

        impacto.MapGet("/", async (IImpactoRepository repositorio) => Results.Ok(await repositorio.ObterResumoAsync()))
            .WithSummary("Painel de impacto: doações concluídas, kg, kcal, refeições estimadas e beneficiários")
            .Produces<ImpactoResponse>();

        impacto.MapGet("/doadores/{id:long}", async (long id, IImpactoRepository repositorio) =>
            {
                var doador = await repositorio.ObterDoadorAsync(id);
                return doador is null
                    ? Results.Problem($"Doador {id} não encontrado", statusCode: StatusCodes.Status404NotFound, title: "Não encontrado")
                    : Results.Ok(doador);
            })
            .WithSummary("Impacto de um doador e o selo solidário (usa pkg_elo_impacto.fn_total_kg_doado)")
            .Produces<ImpactoDoadorResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        var doacoes = app.MapGroup("/doacoes").WithTags("Anti-desperdício").RequireAuthorization();

        doacoes.MapGet("/urgentes", async (IImpactoRepository repositorio, int dias = 3, string? bairro = null) =>
            {
                if (dias is < 0 or > 30)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["dias"] = new[] { "Informe um número de dias entre 0 e 30" }
                    }, title: "Parâmetros inválidos");
                }
                return Results.Ok(await repositorio.ListarUrgentesAsync(dias, bairro));
            })
            .WithSummary("Doações disponíveis que vencem em até N dias, para serem retiradas primeiro (usa pkg_elo_doacao.fn_dias_para_vencer)")
            .Produces<IReadOnlyList<DoacaoUrgenteResponse>>()
            .ProducesValidationProblem();

        app.MapPost("/admin/doacoes/expirar", async (IImpactoRepository repositorio) =>
                Results.Ok(new ExpiracaoResponse(await repositorio.ExpirarDoacoesAsync(), DateTime.Now)))
            .WithTags("Administração")
            .RequireAuthorization(Politicas.Admin)
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaEscrita)
            .ComIdempotencia()
            .WithSummary("Marca como vencidas as doações disponíveis com alimento fora da validade (executa pkg_elo_impacto.prc_expirar_doacoes)")
            .Produces<ExpiracaoResponse>();
    }
}

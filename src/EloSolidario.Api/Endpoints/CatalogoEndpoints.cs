using System.Security.Claims;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Api.Infra.Idempotencia;
using EloSolidario.Api.Infra.LimiteRequisicoes;
using EloSolidario.Api.Infra.Validacao;
using EloSolidario.Api.Services;

namespace EloSolidario.Api.Endpoints;

public static class CatalogoEndpoints
{
    public static void MapCatalogoEndpoints(this WebApplication app)
    {
        var categorias = app.MapGroup("/categorias").WithTags("Categorias").RequireAuthorization();

        categorias.MapGet("/", async (CatalogoService service) => Results.Ok(await service.ListarCategoriasAsync()))
            .WithSummary("Lista as categorias de alimento")
            .Produces<IReadOnlyList<CategoriaResponse>>();

        categorias.MapPost("/", CriarCategoria)
            .RequireAuthorization(Politicas.Admin)
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaEscrita)
            .ComValidacao<CategoriaRequest>()
            .ComIdempotencia()
            .WithSummary("Cadastra uma categoria (somente ADMIN)")
            .Produces<CategoriaResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var alimentos = app.MapGroup("/alimentos").WithTags("Alimentos").RequireAuthorization();

        alimentos.MapGet("/", ListarAlimentos)
            .WithSummary("Catálogo paginado, com filtros por categoria, nome e conservação")
            .Produces<PaginaResponse<AlimentoResponse>>()
            .ProducesValidationProblem();

        alimentos.MapGet("/{id:long}", BuscarAlimento)
            .WithSummary("Busca um alimento, com a orientação de conservação")
            .Produces<AlimentoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        alimentos.MapPost("/", CriarAlimento)
            .RequireAuthorization(Politicas.Admin)
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaEscrita)
            .ComValidacao<AlimentoRequest>()
            .ComIdempotencia()
            .WithSummary("Cadastra um alimento no catálogo (somente ADMIN)")
            .Produces<AlimentoResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        alimentos.MapPut("/{id:long}", AtualizarAlimento)
            .RequireAuthorization(Politicas.Admin)
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaEscrita)
            .ComValidacao<AlimentoRequest>()
            .WithSummary("Atualiza um alimento (somente ADMIN). PUT é idempotente por natureza")
            .Produces<AlimentoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        alimentos.MapPatch("/{id:long}/ativo", AlterarAtivo)
            .RequireAuthorization(Politicas.Admin)
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaEscrita)
            .ComValidacao<AtivoRequest>()
            .WithSummary("Ativa ou desativa um alimento no catálogo (somente ADMIN)")
            .Produces<AlimentoResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        alimentos.MapDelete("/{id:long}", ExcluirAlimento)
            .RequireAuthorization(Politicas.Admin)
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaEscrita)
            .WithSummary("Exclui um alimento que nunca foi usado em doações (somente ADMIN)")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CriarCategoria(CategoriaRequest request, CatalogoService service)
    {
        var resultado = await service.CriarCategoriaAsync(request);
        return resultado.Sucesso
            ? Results.Created($"/categorias/{resultado.Valor!.Id}", resultado.Valor)
            : resultado.ParaErroHttp();
    }

    private static async Task<IResult> ListarAlimentos(CatalogoService service, ClaimsPrincipal usuario,
        long? idCategoria, string? nome, Conservacao? conservacao, bool incluirInativos = false, int pagina = 1, int tamanho = 10)
    {
        var erros = new Dictionary<string, string[]>();
        if (pagina < 1) erros["pagina"] = new[] { "A página deve ser maior ou igual a 1" };
        if (tamanho is < 1 or > 50) erros["tamanho"] = new[] { "O tamanho deve estar entre 1 e 50" };
        if (incluirInativos && !usuario.IsInRole(Perfil.Admin)) erros["incluirInativos"] = new[] { "Somente ADMIN pode listar alimentos inativos" };
        if (erros.Count > 0) return Results.ValidationProblem(erros, title: "Parâmetros inválidos");

        return Results.Ok(await service.ListarAlimentosAsync(idCategoria, nome, conservacao, incluirInativos, pagina, tamanho));
    }

    private static async Task<IResult> BuscarAlimento(long id, CatalogoService service)
    {
        var alimento = await service.BuscarAlimentoAsync(id);
        return alimento is null
            ? Results.Problem($"Alimento {id} não encontrado", statusCode: StatusCodes.Status404NotFound, title: "Não encontrado")
            : Results.Ok(alimento);
    }

    private static async Task<IResult> CriarAlimento(AlimentoRequest request, CatalogoService service)
    {
        var resultado = await service.CriarAlimentoAsync(request);
        return resultado.Sucesso
            ? Results.Created($"/alimentos/{resultado.Valor!.Id}", resultado.Valor)
            : resultado.ParaErroHttp();
    }

    private static async Task<IResult> AtualizarAlimento(long id, AlimentoRequest request, CatalogoService service)
    {
        var resultado = await service.AtualizarAlimentoAsync(id, request);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : resultado.ParaErroHttp();
    }

    private static async Task<IResult> AlterarAtivo(long id, AtivoRequest request, CatalogoService service)
    {
        var resultado = await service.AlterarAtivoAsync(id, request.Ativo!.Value);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : resultado.ParaErroHttp();
    }

    private static async Task<IResult> ExcluirAlimento(long id, CatalogoService service)
    {
        var resultado = await service.ExcluirAlimentoAsync(id);
        return resultado.Sucesso ? Results.NoContent() : resultado.ParaErroHttp();
    }
}

using Microsoft.AspNetCore.Diagnostics;

namespace EloSolidario.Api.Infra.Erros;

public class TratadorExcecoes : IExceptionHandler
{
    private readonly ILogger<TratadorExcecoes> _logger;

    public TratadorExcecoes(ILogger<TratadorExcecoes> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        IResult resposta;

        if (exception is BadHttpRequestException requisicaoInvalida)
        {
            resposta = Results.Problem(
                "O corpo ou os parâmetros da requisição estão em formato inválido. Confira os tipos e os valores permitidos.",
                statusCode: requisicaoInvalida.StatusCode,
                title: "Requisição inválida");
        }
        else
        {
            _logger.LogError(exception, "Erro inesperado em {Caminho}", httpContext.Request.Path);
            resposta = Results.Problem("Ocorreu um erro inesperado no servidor.",
                statusCode: StatusCodes.Status500InternalServerError, title: "Erro interno");
        }

        await resposta.ExecuteAsync(httpContext);
        return true;
    }
}

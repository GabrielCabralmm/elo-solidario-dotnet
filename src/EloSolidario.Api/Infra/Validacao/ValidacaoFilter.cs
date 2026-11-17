using System.ComponentModel.DataAnnotations;

namespace EloSolidario.Api.Infra.Validacao;

public class ValidacaoFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var objeto = context.Arguments.OfType<T>().FirstOrDefault();

        if (objeto is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["corpo"] = new[] { "O corpo da requisição é obrigatório" }
            });
        }

        var resultados = new List<ValidationResult>();
        if (!Validator.TryValidateObject(objeto, new ValidationContext(objeto), resultados, validateAllProperties: true))
        {
            var erros = resultados
                .SelectMany(r => r.MemberNames.DefaultIfEmpty("corpo").Select(campo => new { campo, r.ErrorMessage }))
                .GroupBy(e => char.ToLowerInvariant(e.campo[0]) + e.campo[1..])
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage ?? "Valor inválido").ToArray());
            return Results.ValidationProblem(erros, title: "Dados inválidos");
        }

        return await next(context);
    }
}

public static class ValidacaoExtensions
{
    public static RouteHandlerBuilder ComValidacao<T>(this RouteHandlerBuilder builder) where T : class
    {
        return builder.AddEndpointFilter<ValidacaoFilter<T>>().ProducesValidationProblem();
    }
}

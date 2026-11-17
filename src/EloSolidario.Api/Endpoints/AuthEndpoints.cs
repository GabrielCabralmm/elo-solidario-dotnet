using System.Security.Claims;
using EloSolidario.Api.Dtos;
using EloSolidario.Api.Infra.LimiteRequisicoes;
using EloSolidario.Api.Infra.Validacao;
using EloSolidario.Api.Services;

namespace EloSolidario.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/auth").WithTags("Autenticação");

        grupo.MapPost("/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting(LimiteRequisicoesConfig.PoliticaLogin)
            .ComValidacao<LoginRequest>()
            .WithSummary("Autentica com e-mail e senha e devolve o JWT (o mesmo token vale na API Java)")
            .Produces<TokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        grupo.MapGet("/me", Me)
            .RequireAuthorization()
            .WithSummary("Dados do usuário dono do token")
            .Produces<UsuarioResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> Login(LoginRequest request, AuthService authService)
    {
        var resposta = await authService.LoginAsync(request);
        return resposta is null
            ? Results.Problem("E-mail ou senha inválidos", statusCode: StatusCodes.Status401Unauthorized, title: "Não autorizado")
            : Results.Ok(resposta);
    }

    private static async Task<IResult> Me(ClaimsPrincipal usuario, AuthService authService)
    {
        var email = usuario.FindFirst("sub")?.Value ?? string.Empty;
        var resposta = await authService.BuscarPorEmailAsync(email);
        return resposta is null ? Results.NotFound() : Results.Ok(resposta);
    }
}

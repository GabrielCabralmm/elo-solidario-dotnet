using EloSolidario.Api.Dtos;
using EloSolidario.Api.Services;

namespace EloSolidario.Tests.Apoio;

public class ImpactoRepositoryFalso : IImpactoRepository
{
    public int ExecucoesExpiracao { get; private set; }

    public Task<ImpactoResponse> ObterResumoAsync() => Task.FromResult(new ImpactoResponse(5, 5, 27.25m, 60516.5m, 3, 100,
        new[] { new ImpactoCategoriaResponse("Laticínios", 1, 12m, 7320m) }));

    public Task<ImpactoDoadorResponse?> ObterDoadorAsync(long idDoador) => Task.FromResult(idDoador == 1
        ? new ImpactoDoadorResponse(1, "Ana Souza", 2, 10.9m, 41566m, RegrasImpacto.SeloDoador(10.9m))
        : null);

    public Task<IReadOnlyList<DoacaoUrgenteResponse>> ListarUrgentesAsync(int dias, string? bairro) =>
        Task.FromResult<IReadOnlyList<DoacaoUrgenteResponse>>(new[]
        {
            new DoacaoUrgenteResponse(7, "Banana prata madura", "Pinheiros", "Mercado", "Banana prata", DateTime.Today.AddDays(1), 1,
                RegrasImpacto.AlertaValidade(1))
        });

    public Task<int> ExpirarDoacoesAsync()
    {
        ExecucoesExpiracao++;
        return Task.FromResult(1);
    }
}

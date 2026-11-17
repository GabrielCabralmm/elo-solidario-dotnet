using EloSolidario.Api.Dtos;

namespace EloSolidario.Api.Services;

public interface IImpactoRepository
{
    Task<ImpactoResponse> ObterResumoAsync();
    Task<ImpactoDoadorResponse?> ObterDoadorAsync(long idDoador);
    Task<IReadOnlyList<DoacaoUrgenteResponse>> ListarUrgentesAsync(int dias, string? bairro);
    Task<int> ExpirarDoacoesAsync();
}

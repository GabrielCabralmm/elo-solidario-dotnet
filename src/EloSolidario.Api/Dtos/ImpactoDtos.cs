namespace EloSolidario.Api.Dtos;

public record ImpactoCategoriaResponse(string Categoria, int ItensEntregues, decimal TotalKg, decimal TotalKcal);

public record ImpactoResponse(int DoacoesConcluidas, int DoacoesDisponiveis, decimal AlimentosDestinadosKg,
    decimal EnergiaEstimadaKcal, int BeneficiariosAtendidos, decimal RefeicoesEstimadas,
    IReadOnlyList<ImpactoCategoriaResponse> PorCategoria);

public record ImpactoDoadorResponse(long IdDoador, string Nome, int DoacoesConcluidas, decimal TotalKgDoado,
    decimal EnergiaEstimadaKcal, string Selo);

public record DoacaoUrgenteResponse(long IdDoacao, string Titulo, string Bairro, string LocalRetirada,
    string Alimentos, DateTime VenceEm, int DiasParaVencer, string Alerta);

public record ExpiracaoResponse(int DoacoesExpiradas, DateTime ExecutadoEm);

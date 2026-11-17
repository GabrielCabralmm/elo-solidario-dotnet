namespace EloSolidario.Api.Dtos;

public record PaginaResponse<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int TotalItens, int TotalPaginas);

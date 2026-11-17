namespace EloSolidario.Api.Infra.Erros;

public enum TipoErro
{
    Nenhum,
    NaoEncontrado,
    Conflito
}

public record ResultadoServico<T>(T? Valor, TipoErro Erro, string? Mensagem)
{
    public bool Sucesso => Erro == TipoErro.Nenhum;

    public static ResultadoServico<T> Ok(T valor) => new(valor, TipoErro.Nenhum, null);
    public static ResultadoServico<T> NaoEncontrado(string mensagem) => new(default, TipoErro.NaoEncontrado, mensagem);
    public static ResultadoServico<T> Conflito(string mensagem) => new(default, TipoErro.Conflito, mensagem);

    public IResult ParaErroHttp() => Erro switch
    {
        TipoErro.NaoEncontrado => Results.Problem(Mensagem, statusCode: StatusCodes.Status404NotFound, title: "Não encontrado"),
        _ => Results.Problem(Mensagem, statusCode: StatusCodes.Status409Conflict, title: "Conflito com a regra de negócio")
    };
}

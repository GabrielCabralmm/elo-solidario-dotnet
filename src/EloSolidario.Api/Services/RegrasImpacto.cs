namespace EloSolidario.Api.Services;

public static class RegrasImpacto
{
    public const decimal KcalPorRefeicao = 600m;

    public static string SeloDoador(decimal totalKg) => totalKg switch
    {
        >= 10 => "OURO",
        >= 5 => "PRATA",
        > 0 => "BRONZE",
        _ => "SEM ENTREGAS"
    };

    public static string AlertaValidade(int diasParaVencer) => diasParaVencer switch
    {
        <= 0 => "VENCE HOJE",
        1 => "VENCE AMANHÃ",
        <= 3 => "URGENTE",
        _ => "ATENÇÃO"
    };

    public static decimal RefeicoesEstimadas(decimal totalKcal) =>
        Math.Floor(totalKcal / KcalPorRefeicao);
}

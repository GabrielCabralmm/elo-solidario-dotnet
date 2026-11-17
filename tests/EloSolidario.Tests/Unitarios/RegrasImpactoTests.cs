using EloSolidario.Api.Services;

namespace EloSolidario.Tests.Unitarios;

public class RegrasImpactoTests
{
    [Theory]
    [InlineData(0, "SEM ENTREGAS")]
    [InlineData(0.9, "BRONZE")]
    [InlineData(5, "PRATA")]
    [InlineData(9.99, "PRATA")]
    [InlineData(10, "OURO")]
    public void SeloDoador_DeveSeguirAsFaixasDoBlocoPlSql(decimal totalKg, string seloEsperado)
    {
        Assert.Equal(seloEsperado, RegrasImpacto.SeloDoador(totalKg));
    }

    [Theory]
    [InlineData(0, "VENCE HOJE")]
    [InlineData(1, "VENCE AMANHÃ")]
    [InlineData(3, "URGENTE")]
    [InlineData(7, "ATENÇÃO")]
    public void AlertaValidade_DeveClassificarPelaProximidadeDoVencimento(int dias, string alertaEsperado)
    {
        Assert.Equal(alertaEsperado, RegrasImpacto.AlertaValidade(dias));
    }

    [Fact]
    public void RefeicoesEstimadas_DeveConsiderar600KcalPorRefeicaoArredondandoParaBaixo()
    {
        Assert.Equal(100m, RegrasImpacto.RefeicoesEstimadas(60516.5m));
        Assert.Equal(0m, RegrasImpacto.RefeicoesEstimadas(599m));
    }
}

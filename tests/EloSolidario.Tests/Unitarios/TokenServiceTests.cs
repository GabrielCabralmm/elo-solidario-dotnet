using System.IdentityModel.Tokens.Jwt;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Services;
using Microsoft.Extensions.Configuration;

namespace EloSolidario.Tests.Unitarios;

public class TokenServiceTests
{
    private static TokenService Criar(string segredo) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = segredo })
        .Build());

    [Fact]
    public void Gerar_DeveConterEmissorEmailIdEPerfil()
    {
        var service = Criar("segredo-de-teste-com-mais-de-32-caracteres!");
        var token = service.Gerar(new Usuario { Id = 7, Email = "maria@email.com", Perfil = Perfil.Beneficiario });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("elo-solidario", jwt.Issuer);
        Assert.Equal("maria@email.com", jwt.Subject);
        Assert.Equal("7", jwt.Claims.First(c => c.Type == "id").Value);
        Assert.Equal("BENEFICIARIO", jwt.Claims.First(c => c.Type == "perfil").Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(100));
    }

    [Fact]
    public void Construtor_ComSegredoCurto_DeveFalhar()
    {
        Assert.Throws<InvalidOperationException>(() => Criar("curto"));
    }
}

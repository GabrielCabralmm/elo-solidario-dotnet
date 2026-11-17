using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EloSolidario.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace EloSolidario.Api.Services;

public class TokenService
{
    public const string Emissor = "elo-solidario";
    public const string ClaimPerfil = "perfil";
    public const string ClaimId = "id";

    private readonly SymmetricSecurityKey _chave;
    private readonly int _expiracaoMinutos;

    public TokenService(IConfiguration configuration)
    {
        var segredo = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(segredo) || Encoding.UTF8.GetByteCount(segredo) < 32)
        {
            throw new InvalidOperationException("Configure Jwt:Secret com pelo menos 32 caracteres");
        }

        _chave = CriarChave(segredo);
        _expiracaoMinutos = configuration.GetValue("Jwt:ExpiracaoMinutos", 120);
    }

    public int ExpiracaoSegundos => _expiracaoMinutos * 60;

    public static SymmetricSecurityKey CriarChave(string segredo) => new(Encoding.UTF8.GetBytes(segredo));

    public string Gerar(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
            new Claim(ClaimId, usuario.Id.ToString(), ClaimValueTypes.Integer64),
            new Claim(ClaimPerfil, usuario.Perfil)
        };

        var token = new JwtSecurityToken(
            issuer: Emissor,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_expiracaoMinutos),
            signingCredentials: new SigningCredentials(_chave, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

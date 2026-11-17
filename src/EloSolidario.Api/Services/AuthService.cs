using EloSolidario.Api.Data;
using EloSolidario.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EloSolidario.Api.Services;

public class AuthService
{
    private readonly EloDbContext _db;
    private readonly TokenService _tokenService;

    public AuthService(EloDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
        {
            return null;
        }

        return new TokenResponse(_tokenService.Gerar(usuario), "Bearer", _tokenService.ExpiracaoSegundos,
            new UsuarioResponse(usuario.Id, usuario.Nome, usuario.Email, usuario.Perfil, usuario.Bairro));
    }

    public async Task<UsuarioResponse?> BuscarPorEmailAsync(string email)
    {
        return await _db.Usuarios.AsNoTracking()
            .Where(u => u.Email == email)
            .Select(u => new UsuarioResponse(u.Id, u.Nome, u.Email, u.Perfil, u.Bairro))
            .FirstOrDefaultAsync();
    }
}

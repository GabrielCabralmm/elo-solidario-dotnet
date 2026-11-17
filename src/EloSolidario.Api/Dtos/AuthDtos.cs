using System.ComponentModel.DataAnnotations;

namespace EloSolidario.Api.Dtos;

public record LoginRequest
{
    [Required(ErrorMessage = "O e-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória")]
    public string Senha { get; init; } = string.Empty;
}

public record UsuarioResponse(long Id, string Nome, string Email, string Perfil, string Bairro);

public record TokenResponse(string Token, string Tipo, int ExpiraEmSegundos, UsuarioResponse Usuario);

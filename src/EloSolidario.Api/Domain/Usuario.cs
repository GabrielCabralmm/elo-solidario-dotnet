namespace EloSolidario.Api.Domain;

public class Usuario
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
}

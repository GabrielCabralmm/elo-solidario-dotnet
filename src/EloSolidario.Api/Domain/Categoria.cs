namespace EloSolidario.Api.Domain;

public class Categoria
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Perecivel { get; set; }
    public List<Alimento> Alimentos { get; set; } = new();
}

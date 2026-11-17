namespace EloSolidario.Api.Domain;

public class Alimento
{
    public long Id { get; set; }
    public long IdCategoria { get; set; }
    public Categoria? Categoria { get; set; }
    public string Nome { get; set; } = string.Empty;
    public Conservacao Conservacao { get; set; }
    public decimal Calorias100g { get; set; }
    public bool Ativo { get; set; } = true;
}

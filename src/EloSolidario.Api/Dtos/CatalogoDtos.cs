using System.ComponentModel.DataAnnotations;
using EloSolidario.Api.Domain;

namespace EloSolidario.Api.Dtos;

public record CategoriaRequest
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(60, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 60 caracteres")]
    public string Nome { get; init; } = string.Empty;

    [Required(ErrorMessage = "Informe se a categoria é perecível")]
    public bool? Perecivel { get; init; }
}

public record CategoriaResponse(long Id, string Nome, bool Perecivel, int QuantidadeAlimentos);

public record AlimentoRequest
{
    [Required(ErrorMessage = "A categoria é obrigatória")]
    [Range(1, long.MaxValue, ErrorMessage = "Categoria inválida")]
    public long? IdCategoria { get; init; }

    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 80 caracteres")]
    public string Nome { get; init; } = string.Empty;

    [Required(ErrorMessage = "A conservação é obrigatória (AMBIENTE, REFRIGERADO ou CONGELADO)")]
    [EnumDataType(typeof(Conservacao), ErrorMessage = "Conservação inválida")]
    public Conservacao? Conservacao { get; init; }

    [Required(ErrorMessage = "As calorias por 100 g são obrigatórias")]
    [Range(0, 900, ErrorMessage = "As calorias por 100 g devem estar entre 0 e 900")]
    public decimal? Calorias100g { get; init; }
}

public record AtivoRequest
{
    [Required(ErrorMessage = "Informe true ou false")]
    public bool? Ativo { get; init; }
}

public record AlimentoResponse(long Id, string Nome, long IdCategoria, string Categoria, bool Perecivel,
    Conservacao Conservacao, decimal Calorias100g, bool Ativo, string OrientacaoConservacao);

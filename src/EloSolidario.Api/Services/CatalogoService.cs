using EloSolidario.Api.Data;
using EloSolidario.Api.Domain;
using EloSolidario.Api.Dtos;
using EloSolidario.Api.Infra.Erros;
using Microsoft.EntityFrameworkCore;

namespace EloSolidario.Api.Services;

public class CatalogoService
{
    private readonly EloDbContext _db;

    public CatalogoService(EloDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CategoriaResponse>> ListarCategoriasAsync()
    {
        return await _db.Categorias.AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse(c.Id, c.Nome, c.Perecivel, c.Alimentos.Count))
            .ToListAsync();
    }

    public async Task<ResultadoServico<CategoriaResponse>> CriarCategoriaAsync(CategoriaRequest request)
    {
        var nome = request.Nome.Trim();
        if (await _db.Categorias.AnyAsync(c => c.Nome.ToLower() == nome.ToLower()))
        {
            return ResultadoServico<CategoriaResponse>.Conflito($"Já existe a categoria '{nome}'");
        }

        var categoria = new Categoria { Nome = nome, Perecivel = request.Perecivel!.Value };
        _db.Categorias.Add(categoria);
        await _db.SaveChangesAsync();

        return ResultadoServico<CategoriaResponse>.Ok(new CategoriaResponse(categoria.Id, categoria.Nome, categoria.Perecivel, 0));
    }

    public async Task<PaginaResponse<AlimentoResponse>> ListarAlimentosAsync(long? idCategoria, string? nome,
        Conservacao? conservacao, bool incluirInativos, int pagina, int tamanho)
    {
        var consulta = _db.Alimentos.AsNoTracking().Include(a => a.Categoria).AsQueryable();

        if (!incluirInativos)
        {
            consulta = consulta.Where(a => a.Ativo);
        }
        if (idCategoria.HasValue)
        {
            consulta = consulta.Where(a => a.IdCategoria == idCategoria.Value);
        }
        if (!string.IsNullOrWhiteSpace(nome))
        {
            var filtro = nome.Trim().ToLower();
            consulta = consulta.Where(a => a.Nome.ToLower().Contains(filtro));
        }
        if (conservacao.HasValue)
        {
            consulta = consulta.Where(a => a.Conservacao == conservacao.Value);
        }

        var total = await consulta.CountAsync();
        var itens = await consulta
            .OrderBy(a => a.Nome)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync();

        return new PaginaResponse<AlimentoResponse>(itens.Select(ParaResponse).ToList(), pagina, tamanho, total,
            (int)Math.Ceiling(total / (double)tamanho));
    }

    public async Task<AlimentoResponse?> BuscarAlimentoAsync(long id)
    {
        var alimento = await _db.Alimentos.AsNoTracking().Include(a => a.Categoria).FirstOrDefaultAsync(a => a.Id == id);
        return alimento is null ? null : ParaResponse(alimento);
    }

    public async Task<ResultadoServico<AlimentoResponse>> CriarAlimentoAsync(AlimentoRequest request)
    {
        var alimento = new Alimento { Ativo = true };
        var erro = await PreencherAlimentoAsync(alimento, request);
        if (erro is not null)
        {
            return erro;
        }

        _db.Alimentos.Add(alimento);
        await _db.SaveChangesAsync();
        return ResultadoServico<AlimentoResponse>.Ok(ParaResponse(alimento));
    }

    public async Task<ResultadoServico<AlimentoResponse>> AtualizarAlimentoAsync(long id, AlimentoRequest request)
    {
        var alimento = await _db.Alimentos.FirstOrDefaultAsync(a => a.Id == id);
        if (alimento is null)
        {
            return ResultadoServico<AlimentoResponse>.NaoEncontrado($"Alimento {id} não encontrado");
        }

        var erro = await PreencherAlimentoAsync(alimento, request);
        if (erro is not null)
        {
            return erro;
        }

        await _db.SaveChangesAsync();
        return ResultadoServico<AlimentoResponse>.Ok(ParaResponse(alimento));
    }

    public async Task<ResultadoServico<AlimentoResponse>> AlterarAtivoAsync(long id, bool ativo)
    {
        var alimento = await _db.Alimentos.Include(a => a.Categoria).FirstOrDefaultAsync(a => a.Id == id);
        if (alimento is null)
        {
            return ResultadoServico<AlimentoResponse>.NaoEncontrado($"Alimento {id} não encontrado");
        }

        alimento.Ativo = ativo;
        await _db.SaveChangesAsync();
        return ResultadoServico<AlimentoResponse>.Ok(ParaResponse(alimento));
    }

    public async Task<ResultadoServico<bool>> ExcluirAlimentoAsync(long id)
    {
        var alimento = await _db.Alimentos.FirstOrDefaultAsync(a => a.Id == id);
        if (alimento is null)
        {
            return ResultadoServico<bool>.NaoEncontrado($"Alimento {id} não encontrado");
        }

        if (await _db.ItensDoacao.AnyAsync(i => i.IdAlimento == id))
        {
            return ResultadoServico<bool>.Conflito(
                "O alimento já foi usado em doações e não pode ser excluído. Desative-o em PATCH /alimentos/{id}/ativo");
        }

        _db.Alimentos.Remove(alimento);
        await _db.SaveChangesAsync();
        return ResultadoServico<bool>.Ok(true);
    }

    public static string OrientacaoPara(Conservacao conservacao) => conservacao switch
    {
        Conservacao.REFRIGERADO => "Manter entre 0 °C e 5 °C e transportar em bolsa térmica, sem deixar fora da geladeira por mais de 2 horas.",
        Conservacao.CONGELADO => "Manter a -18 °C ou menos e transportar em caixa térmica com gelo. Não recongelar depois de descongelado.",
        _ => "Guardar em local seco, arejado e longe do sol, com a embalagem fechada."
    };

    private async Task<ResultadoServico<AlimentoResponse>?> PreencherAlimentoAsync(Alimento alimento, AlimentoRequest request)
    {
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == request.IdCategoria);
        if (categoria is null)
        {
            return ResultadoServico<AlimentoResponse>.NaoEncontrado($"Categoria {request.IdCategoria} não encontrada");
        }

        var nome = request.Nome.Trim();
        var duplicado = await _db.Alimentos.AnyAsync(a => a.Id != alimento.Id && a.Nome.ToLower() == nome.ToLower());
        if (duplicado)
        {
            return ResultadoServico<AlimentoResponse>.Conflito($"Já existe o alimento '{nome}' no catálogo");
        }

        alimento.Nome = nome;
        alimento.IdCategoria = categoria.Id;
        alimento.Categoria = categoria;
        alimento.Conservacao = request.Conservacao!.Value;
        alimento.Calorias100g = request.Calorias100g!.Value;
        return null;
    }

    private static AlimentoResponse ParaResponse(Alimento a) => new(a.Id, a.Nome, a.IdCategoria,
        a.Categoria?.Nome ?? string.Empty, a.Categoria?.Perecivel ?? false, a.Conservacao, a.Calorias100g, a.Ativo,
        OrientacaoPara(a.Conservacao));
}

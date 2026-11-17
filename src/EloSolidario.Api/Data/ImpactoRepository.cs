using System.Data;
using System.Data.Common;
using EloSolidario.Api.Dtos;
using EloSolidario.Api.Services;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;

namespace EloSolidario.Api.Data;

public class ImpactoRepository : IImpactoRepository
{
    private readonly EloDbContext _db;

    public ImpactoRepository(EloDbContext db)
    {
        _db = db;
    }

    public async Task<ImpactoResponse> ObterResumoAsync()
    {
        const string sqlCategorias = """
            SELECT c.nome, COUNT(*), SUM(i.peso_kg), SUM(i.peso_kg * 10 * a.calorias_100g)
              FROM tb_elo_item_doacao i
              JOIN tb_elo_doacao d ON d.id_doacao = i.id_doacao
              JOIN tb_elo_alimento a ON a.id_alimento = i.id_alimento
              JOIN tb_elo_categoria c ON c.id_categoria = a.id_categoria
             WHERE d.status = 'CONCLUIDA'
             GROUP BY c.nome
             ORDER BY SUM(i.peso_kg) DESC
            """;

        const string sqlTotais = """
            SELECT (SELECT COUNT(*) FROM tb_elo_doacao WHERE status = 'CONCLUIDA'),
                   (SELECT COUNT(*) FROM tb_elo_doacao WHERE status = 'DISPONIVEL'),
                   (SELECT COUNT(DISTINCT id_beneficiario) FROM tb_elo_reserva WHERE status = 'CONCLUIDA')
              FROM dual
            """;

        var categorias = new List<ImpactoCategoriaResponse>();
        int concluidas = 0, disponiveis = 0, beneficiarios = 0;

        await ExecutarAsync(async conexao =>
        {
            await using (var comando = Criar(conexao, sqlCategorias))
            await using (var leitor = await comando.ExecuteReaderAsync())
            {
                while (await leitor.ReadAsync())
                {
                    categorias.Add(new ImpactoCategoriaResponse(leitor.GetString(0), Convert.ToInt32(leitor.GetValue(1)),
                        Convert.ToDecimal(leitor.GetValue(2)), Math.Round(Convert.ToDecimal(leitor.GetValue(3)), 1)));
                }
            }

            await using (var comando = Criar(conexao, sqlTotais))
            await using (var leitor = await comando.ExecuteReaderAsync())
            {
                await leitor.ReadAsync();
                concluidas = Convert.ToInt32(leitor.GetValue(0));
                disponiveis = Convert.ToInt32(leitor.GetValue(1));
                beneficiarios = Convert.ToInt32(leitor.GetValue(2));
            }
        });

        var totalKg = categorias.Sum(c => c.TotalKg);
        var totalKcal = categorias.Sum(c => c.TotalKcal);
        return new ImpactoResponse(concluidas, disponiveis, totalKg, totalKcal, beneficiarios,
            RegrasImpacto.RefeicoesEstimadas(totalKcal), categorias);
    }

    public async Task<ImpactoDoadorResponse?> ObterDoadorAsync(long idDoador)
    {
        const string sql = """
            SELECT u.nome,
                   (SELECT COUNT(*) FROM tb_elo_doacao d WHERE d.id_doador = u.id_usuario AND d.status = 'CONCLUIDA'),
                   pkg_elo_impacto.fn_total_kg_doado(u.id_usuario),
                   NVL((SELECT SUM(pkg_elo_impacto.fn_calorias_doacao(d.id_doacao))
                          FROM tb_elo_doacao d
                         WHERE d.id_doador = u.id_usuario AND d.status = 'CONCLUIDA'), 0)
              FROM tb_elo_usuario u
             WHERE u.id_usuario = :id
               AND u.perfil = 'DOADOR'
            """;

        ImpactoDoadorResponse? resposta = null;
        await ExecutarAsync(async conexao =>
        {
            await using var comando = Criar(conexao, sql);
            comando.Parameters.Add(new OracleParameter("id", OracleDbType.Int64) { Value = idDoador });
            await using var leitor = await comando.ExecuteReaderAsync();
            if (await leitor.ReadAsync())
            {
                var totalKg = Convert.ToDecimal(leitor.GetValue(2));
                resposta = new ImpactoDoadorResponse(idDoador, leitor.GetString(0), Convert.ToInt32(leitor.GetValue(1)),
                    totalKg, Convert.ToDecimal(leitor.GetValue(3)), RegrasImpacto.SeloDoador(totalKg));
            }
        });
        return resposta;
    }

    public async Task<IReadOnlyList<DoacaoUrgenteResponse>> ListarUrgentesAsync(int dias, string? bairro)
    {
        const string sql = """
            SELECT * FROM (
                SELECT d.id_doacao, d.titulo, d.bairro, d.local_retirada,
                       (SELECT LISTAGG(a.nome, ', ') WITHIN GROUP (ORDER BY a.nome)
                          FROM tb_elo_item_doacao i JOIN tb_elo_alimento a ON a.id_alimento = i.id_alimento
                         WHERE i.id_doacao = d.id_doacao) AS alimentos,
                       (SELECT MIN(i.dt_validade) FROM tb_elo_item_doacao i WHERE i.id_doacao = d.id_doacao) AS vence_em,
                       pkg_elo_doacao.fn_dias_para_vencer(d.id_doacao) AS dias
                  FROM tb_elo_doacao d
                 WHERE d.status = 'DISPONIVEL'
                   AND EXISTS (SELECT 1 FROM tb_elo_item_doacao i WHERE i.id_doacao = d.id_doacao)
                   AND (:bairro IS NULL OR UPPER(d.bairro) = UPPER(:bairro))
            )
             WHERE dias BETWEEN 0 AND :dias
             ORDER BY dias, id_doacao
            """;

        var lista = new List<DoacaoUrgenteResponse>();
        await ExecutarAsync(async conexao =>
        {
            await using var comando = (OracleCommand)Criar(conexao, sql);
            comando.BindByName = true;
            comando.Parameters.Add(new OracleParameter("bairro", OracleDbType.Varchar2)
            {
                Value = string.IsNullOrWhiteSpace(bairro) ? DBNull.Value : bairro.Trim()
            });
            comando.Parameters.Add(new OracleParameter("dias", OracleDbType.Int32) { Value = dias });

            await using var leitor = await comando.ExecuteReaderAsync();
            while (await leitor.ReadAsync())
            {
                var diasParaVencer = Convert.ToInt32(leitor.GetValue(6));
                lista.Add(new DoacaoUrgenteResponse(Convert.ToInt64(leitor.GetValue(0)), leitor.GetString(1),
                    leitor.GetString(2), leitor.GetString(3), leitor.GetString(4), leitor.GetDateTime(5),
                    diasParaVencer, RegrasImpacto.AlertaValidade(diasParaVencer)));
            }
        });
        return lista;
    }

    public async Task<int> ExpirarDoacoesAsync()
    {
        var quantidade = 0;
        await ExecutarAsync(async conexao =>
        {
            await using var transacao = await conexao.BeginTransactionAsync();
            await using var comando = Criar(conexao, "BEGIN pkg_elo_impacto.prc_expirar_doacoes(:qtd); END;");
            comando.Transaction = transacao;
            var parametro = new OracleParameter("qtd", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            comando.Parameters.Add(parametro);
            await comando.ExecuteNonQueryAsync();
            await transacao.CommitAsync();
            quantidade = Convert.ToInt32(parametro.Value.ToString());
        });
        return quantidade;
    }

    private async Task ExecutarAsync(Func<DbConnection, Task> acao)
    {
        var conexao = _db.Database.GetDbConnection();
        var abriuAqui = conexao.State != ConnectionState.Open;
        if (abriuAqui)
        {
            await conexao.OpenAsync();
        }

        try
        {
            await acao(conexao);
        }
        finally
        {
            if (abriuAqui)
            {
                await conexao.CloseAsync();
            }
        }
    }

    private static DbCommand Criar(DbConnection conexao, string sql)
    {
        var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        return comando;
    }
}

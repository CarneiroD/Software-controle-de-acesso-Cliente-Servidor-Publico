using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using WpfApp1.Core;

namespace WpfApp1.Server
{
    public class HistoricoSessoes
    {
        private readonly string caminhoBanco;

        private readonly object bloqueio =
            new();

        public HistoricoSessoes()
        {
            caminhoBanco =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "historico.db"
                );

            CriarTabelaSeNecessario();
        }

        private SqliteConnection AbrirConexao()
        {
            SqliteConnection conexao =
                new SqliteConnection(
                    $"Data Source={caminhoBanco}"
                );

            conexao.Open();

            return conexao;
        }

        private void CriarTabelaSeNecessario()
        {
            lock (bloqueio)
            {
                using SqliteConnection conexao =
                    AbrirConexao();

                using SqliteCommand comando =
                    conexao.CreateCommand();

                comando.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS Sessoes (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Computador TEXT NOT NULL,
                        InicioUtc TEXT NOT NULL,
                        FimUtc TEXT NULL,
                        DuracaoSegundos INTEGER NULL,
                        Motivo TEXT NULL
                    );
                    """;

                comando.ExecuteNonQuery();
            }
        }

        public void RegistrarInicio(
            string nomeComputador)
        {
            try
            {
                lock (bloqueio)
                {
                    using SqliteConnection conexao =
                        AbrirConexao();

                    using SqliteCommand comando =
                        conexao.CreateCommand();

                    comando.CommandText =
                        """
                        INSERT INTO Sessoes (Computador, InicioUtc)
                        VALUES ($computador, $inicio);
                        """;

                    comando.Parameters.AddWithValue(
                        "$computador",
                        nomeComputador
                    );

                    comando.Parameters.AddWithValue(
                        "$inicio",
                        DateTime.UtcNow.ToString("O")
                    );

                    comando.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao registrar início de sessão: {ex.Message}"
                );
            }
        }

        public void RegistrarFim(
            string nomeComputador,
            string motivo)
        {
            try
            {
                lock (bloqueio)
                {
                    using SqliteConnection conexao =
                        AbrirConexao();

                    using SqliteCommand buscar =
                        conexao.CreateCommand();

                    buscar.CommandText =
                        """
                        SELECT Id, InicioUtc FROM Sessoes
                        WHERE Computador = $computador AND FimUtc IS NULL
                        ORDER BY Id DESC
                        LIMIT 1;
                        """;

                    buscar.Parameters.AddWithValue(
                        "$computador",
                        nomeComputador
                    );

                    long? id = null;
                    DateTime inicio = DateTime.UtcNow;

                    using (SqliteDataReader leitor = buscar.ExecuteReader())
                    {
                        if (leitor.Read())
                        {
                            id = leitor.GetInt64(0);

                            inicio = DateTime.Parse(
                                leitor.GetString(1),
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.RoundtripKind
                            );
                        }
                    }

                    if (id == null)
                        return;

                    DateTime fim =
                        DateTime.UtcNow;

                    int duracaoSegundos =
                        (int)(fim - inicio).TotalSeconds;

                    using SqliteCommand atualizar =
                        conexao.CreateCommand();

                    atualizar.CommandText =
                        """
                        UPDATE Sessoes
                        SET FimUtc = $fim, DuracaoSegundos = $duracao, Motivo = $motivo
                        WHERE Id = $id;
                        """;

                    atualizar.Parameters.AddWithValue(
                        "$fim",
                        fim.ToString("O")
                    );

                    atualizar.Parameters.AddWithValue(
                        "$duracao",
                        duracaoSegundos
                    );

                    atualizar.Parameters.AddWithValue(
                        "$motivo",
                        motivo
                    );

                    atualizar.Parameters.AddWithValue(
                        "$id",
                        id.Value
                    );

                    atualizar.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao registrar fim de sessão: {ex.Message}"
                );
            }
        }

        public List<RegistroSessao> ObterHistorico(
            int limite = 200)
        {
            List<RegistroSessao> resultado =
                new();

            try
            {
                lock (bloqueio)
                {
                    using SqliteConnection conexao =
                        AbrirConexao();

                    using SqliteCommand comando =
                        conexao.CreateCommand();

                    comando.CommandText =
                        """
                        SELECT Id, Computador, InicioUtc, FimUtc, DuracaoSegundos, Motivo
                        FROM Sessoes
                        ORDER BY Id DESC
                        LIMIT $limite;
                        """;

                    comando.Parameters.AddWithValue(
                        "$limite",
                        limite
                    );

                    using SqliteDataReader leitor =
                        comando.ExecuteReader();

                    while (leitor.Read())
                    {
                        resultado.Add(
                            new RegistroSessao
                            {
                                Id =
                                    leitor.GetInt32(0),

                                Computador =
                                    leitor.GetString(1),

                                InicioUtc =
                                    DateTime.Parse(
                                        leitor.GetString(2),
                                        CultureInfo.InvariantCulture,
                                        DateTimeStyles.RoundtripKind
                                    ),

                                FimUtc =
                                    leitor.IsDBNull(3)
                                        ? null
                                        : DateTime.Parse(
                                            leitor.GetString(3),
                                            CultureInfo.InvariantCulture,
                                            DateTimeStyles.RoundtripKind
                                        ),

                                DuracaoSegundos =
                                    leitor.IsDBNull(4)
                                        ? null
                                        : leitor.GetInt32(4),

                                Motivo =
                                    leitor.IsDBNull(5)
                                        ? null
                                        : leitor.GetString(5)
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro ao consultar histórico: {ex.Message}"
                );
            }

            return resultado;
        }
    }
}
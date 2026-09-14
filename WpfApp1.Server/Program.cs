using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WpfApp1.Core;
using System.Text.Json;

namespace WpfApp1.Server
{
    internal class Program
    {
        private const int Porta = 5000;

        private const int TimeoutHeartbeatSegundos = 10;

        private static readonly GerenciadorSessoes gerenciador =
            new GerenciadorSessoes();

        private static readonly HistoricoSessoes historico =
    new HistoricoSessoes();
        private static TcpListener? servidor;

        private class ConexaoCliente
        {
            public string NomeComputador { get; set; } = "";

            public TcpClient Tcp { get; set; } = null!;

            public NetworkStream Stream { get; set; } = null!;

            public StreamReader Leitor { get; set; } = null!;

            public StreamWriter Escritor { get; set; } = null!;

            public DateTime UltimoHeartbeat { get; set; }
                = DateTime.UtcNow;
        }

        private static readonly Dictionary<
            string,
            ConexaoCliente
        > clientes = new();

        private static ConexaoCliente? administrador;

        private static readonly object bloqueioClientes =
            new();

        private static readonly object bloqueioGerenciador =
            new();

        private static volatile bool encerrando = false;

        static void Main(string[] args)
        {
            Console.WriteLine("======================================");
            Console.WriteLine("     SERVIDOR DE CONTROLE DE PCs");
            Console.WriteLine("======================================");
            Console.WriteLine();

            servidor = new TcpListener(
                IPAddress.Any,
                Porta
            );

            servidor.Start();

            Console.WriteLine(
                $"Servidor iniciado na porta {Porta}."
            );

            Console.WriteLine(
                "Aguardando computadores..."
            );

            gerenciador.SessaoIniciada +=
                (sender, e) => historico.RegistrarInicio(e.NomeComputador);

            gerenciador.SessaoEncerrada +=
                (sender, e) => historico.RegistrarFim(e.NomeComputador, e.Motivo);

            Console.WriteLine();

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine(
                    "Sinal de encerramento recebido. Finalizando servidor..."
                );

                encerrando = true;

                e.Cancel = true;

                try
                {
                    servidor?.Stop();
                }
                catch
                {
                }
            };

            Thread relogio =
                new Thread(RelogioSessoes);

            relogio.IsBackground = true;
            relogio.Start();

            Thread monitor =
                new Thread(MonitorarConexoes);

            monitor.IsBackground = true;
            monitor.Start();

            while (!encerrando)
            {
                TcpClient cliente;

                try
                {
                    cliente =
                        servidor.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    break;
                }

                Thread thread =
                    new Thread(() =>
                    {
                        AtenderCliente(cliente);
                    });

                thread.IsBackground = true;
                thread.Start();
            }

            Console.WriteLine(
                "Servidor encerrado."
            );
        }
        private static void RelogioSessoes()
        {
            while (true)
            {
                Thread.Sleep(1000);

                lock (bloqueioGerenciador)
                {
                    gerenciador.AtualizarSessoes();
                }

                EnviarEstadosParaTodos();
            }
        }

        private static void MonitorarConexoes()
        {
            while (true)
            {
                Thread.Sleep(5000);

                List<ConexaoCliente> conexoes;

                lock (bloqueioClientes)
                {
                    conexoes = clientes.Values.ToList();
                }

                foreach (
                    ConexaoCliente conexao
                    in conexoes)
                {
                    TimeSpan tempoSemHeartbeat =
                        DateTime.UtcNow -
                        conexao.UltimoHeartbeat;

                    if (
                        tempoSemHeartbeat.TotalSeconds >
                        TimeoutHeartbeatSegundos)
                    {
                        MarcarComoOffline(
                            conexao.NomeComputador
                        );
                    }
                }

                EnviarEstadosParaTodos();
            }
        }

        private static void AtenderCliente(
            TcpClient clienteTcp)
        {
            NetworkStream stream =
                clienteTcp.GetStream();

            StreamReader leitor =
                new StreamReader(
                    stream
                );

            StreamWriter escritor =
                new StreamWriter(
                    stream
                )
                {
                    AutoFlush = true
                };

            ConexaoCliente conexao =
                new ConexaoCliente
                {
                    Tcp = clienteTcp,
                    Stream = stream,
                    Leitor = leitor,
                    Escritor = escritor
                };

            try
            {
                while (true)
                {
                    string? mensagem =
                        leitor.ReadLine();

                    if (mensagem == null)
                        break;

                    mensagem = mensagem.Trim();

                    if (mensagem.Length == 0)
                        continue;

                    Console.WriteLine(
                        $"Recebido: {mensagem}"
                    );

                    ProcessarMensagem(
                        mensagem,
                        conexao
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Erro na conexão: {ex.Message}"
                );
            }
            finally
            {
                Desconectar(conexao);

                try
                {
                    escritor.Close();
                    leitor.Close();
                    stream.Close();
                    clienteTcp.Close();
                }
                catch
                {
                }
            }
        }

        private static void ProcessarMensagem(
            string mensagem,
            ConexaoCliente conexao)
        {
            string[] partes =
                mensagem.Split('|');

            if (partes.Length < 1)
                return;

            string comando =
                partes[0];

            string identificador =
                partes.Length >= 2
                    ? partes[1]
                    : "";

            switch (comando)
            {
                case "REGISTRAR":

                    if (string.IsNullOrWhiteSpace(identificador))
                        return;

                    RegistrarComputador(
                        identificador,
                        conexao
                    );

                    break;

                case "REGISTRAR_ADMIN":

                    RegistrarAdministrador(
                        conexao
                    );

                    break;

                case "HEARTBEAT":

                    ProcessarHeartbeat(
                        identificador,
                        conexao
                    );

                    break;

                case "STATUS":

                    EnviarEstado(
                        identificador,
                        conexao.Escritor
                    );

                    break;

                case "INICIAR":

                    IniciarSessao(
                        identificador
                    );

                    break;

                case "SOLICITAR_EXTENSAO":

                    SolicitarExtensao(
                        identificador
                    );

                    break;

                case "ADMIN_INICIAR":

                    IniciarSessao(
                        identificador
                    );

                    break;

                case "ADMIN_ENCERRAR":

                    EncerrarSessao(
                        identificador
                    );

                    break;

                case "ADMIN_CONCEDER_EXTENSAO":

                    ConcederExtensao(
                        identificador
                    );

                    break;

                case "ADMIN_RECUSAR_EXTENSAO":

                    RecusarExtensao(
                        identificador
                    );

                    break;

                case "ADMIN_DESBLOQUEAR":

                    DesbloquearComputador(
                        identificador
                    );

                    break;

                default:

                    Console.WriteLine(
                        $"Comando desconhecido: {comando}"
                    );

                    break;

                case "ADMIN_HISTORICO":

                    EnviarHistorico(
                        conexao.Escritor
                    );

                    break;
            }
        }

        private static void RegistrarComputador(
            string nomeComputador,
            ConexaoCliente conexao)
        {
            Computador? computador;

            lock (bloqueioGerenciador)
            {
                computador =
                    gerenciador.Computadores.FirstOrDefault(
                        c => c.Nome == nomeComputador
                    );

                if (computador == null)
                {
                    Console.WriteLine(
                        $"Computador inexistente: {nomeComputador}"
                    );

                    return;
                }

                computador.Online = true;
            }

            conexao.NomeComputador =
                nomeComputador;

            conexao.UltimoHeartbeat =
                DateTime.UtcNow;

            lock (bloqueioClientes)
            {
                clientes[nomeComputador] =
                    conexao;
            }

            Console.WriteLine(
                $"Computador registrado: {nomeComputador}"
            );

            EnviarEstado(
                nomeComputador,
                conexao.Escritor
            );

            EnviarEstadosParaTodos();
        }

        private static void RegistrarAdministrador(
            ConexaoCliente conexao)
        {
            conexao.NomeComputador =
                "ADMIN";

            conexao.UltimoHeartbeat =
                DateTime.UtcNow;

            lock (bloqueioClientes)
            {
                administrador =
                    conexao;
            }

            Console.WriteLine(
                "Painel administrativo conectado."
            );

            EnviarEstadosParaAdministrador();
        }

        private static void ProcessarHeartbeat(
            string nomeComputador,
            ConexaoCliente conexao)
        {
            if (
                !string.IsNullOrWhiteSpace(
                    nomeComputador
                )
            )
            {
                conexao.UltimoHeartbeat =
                    DateTime.UtcNow;
            }

            try
            {
                lock (conexao.Escritor)
                {
                    conexao.Escritor.WriteLine(
                        "HEARTBEAT_OK"
                    );
                }
            }
            catch
            {
            }
        }

        private static void IniciarSessao(
            string nomeComputador)
        {
            bool sucesso;

            lock (bloqueioGerenciador)
            {
                sucesso =
                    gerenciador.IniciarSessao(
                        nomeComputador
                    );
            }

            if (sucesso)
            {
                Console.WriteLine(
                    $"Sessão iniciada: {nomeComputador}"
                );
            }

            EnviarEstadosParaTodos();
        }

        private static void EncerrarSessao(
            string nomeComputador)
        {
            bool sucesso;

            lock (bloqueioGerenciador)
            {
                sucesso =
                    gerenciador.EncerrarSessao(
                        nomeComputador
                    );
            }

            if (sucesso)
            {
                Console.WriteLine(
                    $"Sessão encerrada: {nomeComputador}"
                );
            }

            EnviarEstadosParaTodos();
        }

        private static void SolicitarExtensao(
    string nomeComputador)
        {
            ResultadoSolicitacaoExtensao resultado;

            lock (bloqueioGerenciador)
            {
                resultado =
                    gerenciador.SolicitarExtensao(
                        nomeComputador
                    );
            }

            Console.WriteLine(
                $"Solicitação de extensão ({nomeComputador}): {resultado}"
            );

            if (resultado != ResultadoSolicitacaoExtensao.Sucesso)
            {
                string motivo =
                    resultado switch
                    {
                        ResultadoSolicitacaoExtensao.JaPendente =>
                            "Já existe uma solicitação de extensão pendente para esta sessão.",

                        ResultadoSolicitacaoExtensao.JaUtilizada =>
                            "A extensão de tempo já foi utilizada nesta sessão. Apenas uma solicitação por sessão é permitida.",

                        _ =>
                            "Não foi possível solicitar a extensão no momento."
                    };

                EnviarMensagemParaComputador(
                    nomeComputador,
                    $"EXTENSAO_INDISPONIVEL|{nomeComputador}|{motivo}"
                );
            }

            EnviarEstadosParaTodos();
        }

        private static void ConcederExtensao(
            string nomeComputador)
        {
            bool sucesso;

            lock (bloqueioGerenciador)
            {
                sucesso =
                    gerenciador.ConcederExtensao(
                        nomeComputador
                    );
            }

            Console.WriteLine(
                sucesso
                    ? $"Extensão concedida: {nomeComputador}"
                    : $"Extensão não pôde ser concedida: {nomeComputador}"
            );

            EnviarEstadosParaTodos();
        }

        private static void RecusarExtensao(
    string nomeComputador)
        {
            lock (bloqueioGerenciador)
            {
                gerenciador.RecusarExtensao(
                    nomeComputador
                );
            }

            Console.WriteLine(
                $"Extensão recusada: {nomeComputador}"
            );

            EnviarMensagemParaComputador(
                nomeComputador,
                $"EXTENSAO_RECUSADA|{nomeComputador}"
            );

            EnviarEstadosParaTodos();
        }

        private static void EnviarMensagemParaComputador(
            string nomeComputador,
            string mensagem)
        {
            ConexaoCliente? conexao;

            lock (bloqueioClientes)
            {
                clientes.TryGetValue(
                    nomeComputador,
                    out conexao
                );
            }

            if (conexao == null)
                return;

            try
            {
                lock (conexao.Escritor)
                {
                    conexao.Escritor.WriteLine(
                        mensagem
                    );
                }
            }
            catch
            {
            }
        }

        private static void DesbloquearComputador(
    string nomeComputador)
        {
            bool sucesso;

            lock (bloqueioGerenciador)
            {
                sucesso =
                    gerenciador.DesbloquearComputador(
                        nomeComputador
                    );
            }

            Console.WriteLine(
                sucesso
                    ? $"Computador desbloqueado: {nomeComputador}"
                    : $"Desbloqueio não permitido: {nomeComputador}"
            );

            EnviarEstadosParaTodos();
        }

        private static void Desconectar(
            ConexaoCliente conexao)
        {
            string nome =
                conexao.NomeComputador;

            if (nome == "ADMIN")
            {
                lock (bloqueioClientes)
                {
                    if (
                        administrador != null &&
                        ReferenceEquals(
                            administrador,
                            conexao
                        )
                    )
                    {
                        administrador = null;
                    }
                }

                Console.WriteLine(
                    "Painel administrativo desconectado."
                );

                return;
            }

            if (
                string.IsNullOrWhiteSpace(
                    nome
                )
            )
            {
                return;
            }

            lock (bloqueioGerenciador)
            {
                Computador? computador =
                    gerenciador.Computadores.FirstOrDefault(
                        c => c.Nome == nome
                    );

                if (computador != null)
                {
                    computador.Online = false;
                }
            }

            lock (bloqueioClientes)
            {
                if (
                    clientes.TryGetValue(
                        nome,
                        out ConexaoCliente? atual
                    )
                )
                {
                    if (
                        ReferenceEquals(
                            atual,
                            conexao
                        )
                    )
                    {
                        clientes.Remove(
                            nome
                        );
                    }
                }
            }

            Console.WriteLine(
                $"Computador desconectado: {nome}"
            );

            EnviarEstadosParaTodos();
        }

        private static void MarcarComoOffline(
            string nomeComputador)
        {
            lock (bloqueioGerenciador)
            {
                Computador? computador =
                    gerenciador.Computadores.FirstOrDefault(
                        c => c.Nome == nomeComputador
                    );

                if (computador != null)
                {
                    computador.Online = false;
                }
            }

            Console.WriteLine(
                $"Heartbeat expirado: {nomeComputador}"
            );
        }

        private static void EnviarEstado(
    string nomeComputador,
    StreamWriter escritor)
        {
            Computador? computador;

            lock (bloqueioGerenciador)
            {
                computador =
                    gerenciador.Computadores.FirstOrDefault(
                        c => c.Nome == nomeComputador
                    );

                if (computador == null)
                    return;

                bool extensaoDisponivel =
                    computador.SegundosExtensao <
                    ConfiguracaoSistema.DuracaoExtensaoSegundos;

                string mensagem =
                    $"ESTADO|" +
                    $"{computador.Nome}|" +
                    $"{computador.Estado}|" +
                    $"{computador.SegundosRestantes}|" +
                    $"{computador.ExtensaoSolicitada}|" +
                    $"{extensaoDisponivel}|" +
                    $"{computador.Online}";

                try
                {
                    lock (escritor)
                    {
                        escritor.WriteLine(
                            mensagem
                        );
                    }
                }
                catch
                {
                }
            }
        }

        private static void EnviarHistorico(
    StreamWriter escritor)
        {
            List<RegistroSessao> registros =
                historico.ObterHistorico(200);

            string json =
                JsonSerializer.Serialize(registros);

            try
            {
                lock (escritor)
                {
                    escritor.WriteLine(
                        $"HISTORICO|{json}"
                    );
                }
            }
            catch
            {
            }
        }

        private static void EnviarEstadosParaTodos()
        {
            List<KeyValuePair<
                string,
                ConexaoCliente
            >> conexoes;

            lock (bloqueioClientes)
            {
                conexoes =
                    clientes.ToList();
            }

            foreach (
                KeyValuePair<
                    string,
                    ConexaoCliente
                > conexao
                in conexoes)
            {
                EnviarEstado(
                    conexao.Key,
                    conexao.Value.Escritor
                );
            }

            EnviarEstadosParaAdministrador();
        }

        private static void EnviarEstadosParaAdministrador()
        {
            ConexaoCliente? admin;

            lock (bloqueioClientes)
            {
                admin =
                    administrador;
            }

            if (admin == null)
                return;

            List<Computador> computadores;

            lock (bloqueioGerenciador)
            {
                computadores =
                    gerenciador.Computadores.ToList();
            }

            foreach (
                Computador computador
                in computadores)
            {
                EnviarEstado(
                    computador.Nome,
                    admin.Escritor
                );
            }
        }
    }
}
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using WpfApp1.Core;

namespace WpfApp1.Cliente
{
    public partial class MainWindow : Window
    {
        private readonly string ipServidor =
            ConfiguracaoCliente.IpServidor;

        private const int PortaServidor =
            5000;

        private readonly string nomeComputador =
            ConfiguracaoCliente.NomeComputador;

        private static readonly TimeSpan IntervaloReconexao =
            TimeSpan.FromSeconds(5);

        private TcpClient? cliente;
        private NetworkStream? stream;
        private StreamReader? leitor;
        private StreamWriter? escritor;

        private EstadoComputador estado =
            EstadoComputador.Disponivel;

        private string statusConexao =
            "Conectando...";

        private int segundosRestantes =
            0;

        private bool extensaoSolicitada =
            false;

        private bool extensaoDisponivel =
            true;

        private bool online =
            false;

        private readonly DispatcherTimer timerExibicao;
        private readonly DispatcherTimer timerHeartbeat;

        public MainWindow()
        {
            InitializeComponent();

            NomeComputadorText.Text =
                nomeComputador;

            timerExibicao =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromSeconds(1)
                };

            timerExibicao.Tick +=
                TimerExibicao_Tick;

            timerExibicao.Start();

            timerHeartbeat =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromSeconds(5)
                };

            timerHeartbeat.Tick +=
                TimerHeartbeat_Tick;

            timerHeartbeat.Start();

            BotaoIniciar.IsEnabled =
                false;

            BotaoExtensao.IsEnabled =
                false;

            AtualizarTela();

            _ = ExecutarLoopConexaoAsync();
        }

        private async Task ExecutarLoopConexaoAsync()
        {
            while (true)
            {
                bool conectou =
                    await ConectarAoServidorAsync();

                if (conectou)
                {
                    await ReceberMensagensAsync();
                }

                online = false;

                statusConexao =
                    "Reconectando...";

                AtualizarTela();

                await Task.Delay(
                    IntervaloReconexao
                );
            }
        }

        private void LimparConexaoAnterior()
        {
            try
            {
                escritor?.Close();
                leitor?.Close();
                stream?.Close();
                cliente?.Close();
            }
            catch
            {
            }
        }

        private async Task<bool> ConectarAoServidorAsync()
        {
            try
            {
                LimparConexaoAnterior();

                cliente =
                    new TcpClient();

                await cliente.ConnectAsync(
                    ipServidor,
                    PortaServidor
                );

                stream =
                    cliente.GetStream();

                leitor =
                    new StreamReader(
                        stream,
                        Encoding.UTF8
                    );

                escritor =
                    new StreamWriter(
                        stream,
                        Encoding.UTF8
                    )
                    {
                        AutoFlush = true
                    };

                online = true;

                statusConexao =
                    "Conectado";

                AtualizarTela();

                await EnviarMensagemAsync(
                    $"REGISTRAR|{nomeComputador}"
                );

                return true;
            }
            catch
            {
                online = false;

                statusConexao =
                    "Servidor indisponível";

                AtualizarTela();

                return false;
            }
        }

        private async Task ReceberMensagensAsync()
        {
            if (leitor == null)
                return;

            try
            {
                while (true)
                {
                    string? mensagem =
                        await leitor.ReadLineAsync();

                    if (mensagem == null)
                        break;

                    if (mensagem.Length == 0)
                        continue;

                    ProcessarMensagem(
                        mensagem.Trim()
                    );
                }
            }
            catch
            {
            }

            online = false;

            statusConexao =
                "Conexão perdida";

            Dispatcher.Invoke(
                AtualizarTela
            );
        }

        private void ProcessarMensagem(
            string mensagem)
        {
            if (mensagem == "HEARTBEAT_OK")
                return;

            string[] partes =
                mensagem.Split('|');

            if (partes.Length < 2)
                return;

            if (partes[0] == "EXTENSAO_RECUSADA")
            {
                if (partes[1] != nomeComputador)
                    return;

                Dispatcher.Invoke(() =>
                {
                    extensaoSolicitada = false;

                    AtualizarTela();

                    MessageBox.Show(
                        "Sua solicitação de +30 minutos foi recusada pelo administrador.",
                        "Extensão recusada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                });

                return;
            }

            if (partes[0] == "EXTENSAO_INDISPONIVEL")
            {
                if (partes[1] != nomeComputador)
                    return;

                string motivo =
                    partes.Length >= 3
                        ? partes[2]
                        : "Não foi possível solicitar a extensão.";

                Dispatcher.Invoke(() =>
                {
                    extensaoSolicitada = false;

                    AtualizarTela();

                    MessageBox.Show(
                        motivo,
                        "Extensão indisponível",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                });

                return;
            }

            if (partes.Length < 7)
                return;

            if (partes[0] != "ESTADO")
                return;

            if (
                partes[1] !=
                nomeComputador
            )
            {
                return;
            }

            if (
                !Enum.TryParse(
                    partes[2],
                    out EstadoComputador novoEstado
                )
            )
            {
                return;
            }

            if (
                !int.TryParse(
                    partes[3],
                    out int novoTempo
                )
            )
            {
                return;
            }

            if (
                !bool.TryParse(
                    partes[4],
                    out bool novaExtensao
                )
            )
            {
                return;
            }

            if (
                !bool.TryParse(
                    partes[5],
                    out bool novaExtensaoDisponivel
                )
            )
            {
                return;
            }

            if (
                !bool.TryParse(
                    partes[6],
                    out bool novoOnline
                )
            )
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                estado =
                    novoEstado;

                segundosRestantes =
                    novoTempo;

                extensaoSolicitada =
                    novaExtensao;

                extensaoDisponivel =
                    novaExtensaoDisponivel;

                online =
                    novoOnline;

                AtualizarTela();
            });
        }

        private async void TimerHeartbeat_Tick(
            object? sender,
            EventArgs e)
        {
            if (!online)
                return;

            await EnviarMensagemAsync(
                $"HEARTBEAT|{nomeComputador}"
            );
        }

        private async void BotaoIniciar_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!online)
                return;

            if (estado == EstadoComputador.Bloqueado)
                return;

            await EnviarMensagemAsync(
                $"INICIAR|{nomeComputador}"
            );
        }

        private async void BotaoExtensao_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (estado == EstadoComputador.Bloqueado)
                return;

            if (!extensaoDisponivel)
            {
                MessageBox.Show(
                    "A extensão de tempo já foi utilizada nesta sessão. Apenas uma solicitação por sessão é permitida.",
                    "Extensão indisponível",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                return;
            }

            if (extensaoSolicitada)
                return;

            bool enviado =
                await EnviarMensagemAsync(
                    $"SOLICITAR_EXTENSAO|{nomeComputador}"
                );

            if (!enviado)
                return;

            extensaoSolicitada =
                true;

            AtualizarTela();

            MessageBox.Show(
                "Solicitação de +30 minutos enviada ao administrador.",
                "Extensão solicitada",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private async Task<bool> EnviarMensagemAsync(
            string mensagem)
        {
            if (escritor == null)
                return false;

            try
            {
                await escritor.WriteLineAsync(
                    mensagem
                );

                return true;
            }
            catch
            {
                online = false;

                statusConexao =
                    "Conexão perdida";

                AtualizarTela();

                return false;
            }
        }

        private void TimerExibicao_Tick(
            object? sender,
            EventArgs e)
        {
            AtualizarTela();
        }

        private static string TextoEstado(
            EstadoComputador estado)
        {
            return estado switch
            {
                EstadoComputador.Disponivel => "Disponível",
                EstadoComputador.EmUso => "Em uso",
                EstadoComputador.Bloqueado => "Bloqueado",
                _ => estado.ToString()
            };
        }

        private void AtualizarTela()
        {
            if (estado == EstadoComputador.Bloqueado)
            {
                MostrarTelaBloqueio();
                return;
            }

            MostrarInterfaceNormal();
        }

        private void MostrarInterfaceNormal()
        {
            BloqueioTeclado.Desativar();

            TelaBloqueio.Visibility =
                Visibility.Collapsed;

            InterfaceNormal.Visibility =
                Visibility.Visible;

            WindowState =
                WindowState.Normal;

            WindowStyle =
                WindowStyle.SingleBorderWindow;

            ResizeMode =
                ResizeMode.NoResize;

            Topmost =
                false;

            int horas =
                segundosRestantes /
                3600;

            int minutos =
                (segundosRestantes % 3600) /
                60;

            int segundos =
                segundosRestantes % 60;

            TempoText.Text =
                $"{horas:00}:{minutos:00}:{segundos:00}";

            Brush corEstado =
                !online
                    ? (Brush)Application.Current.Resources["CorTextoSecundario"]
                    : estado == EstadoComputador.EmUso
                        ? (Brush)Application.Current.Resources["CorEmUso"]
                        : (Brush)Application.Current.Resources["CorDisponivel"];

            EstadoText.Foreground = corEstado;

            TempoText.Foreground = corEstado;

            EstadoText.Text =
                online
                    ? TextoEstado(estado).ToUpper()
                    : statusConexao.ToUpper();

            BotaoIniciar.IsEnabled =
                online &&
                estado == EstadoComputador.Disponivel;

            BotaoExtensao.IsEnabled =
                online &&
                estado == EstadoComputador.EmUso &&
                !extensaoSolicitada &&
                extensaoDisponivel &&
                segundosRestantes > 0;

            BotaoExtensao.Content =
                !extensaoDisponivel
                    ? "EXTENSÃO JÁ UTILIZADA"
                    : extensaoSolicitada
                        ? "SOLICITAÇÃO ENVIADA"
                        : "SOLICITAR +30 MINUTOS";
        }

        private void MostrarTelaBloqueio()
        {
            InterfaceNormal.Visibility =
                Visibility.Collapsed;

            TelaBloqueio.Visibility =
                Visibility.Visible;

            if (ConfiguracaoCliente.ModoDesenvolvedor)
            {
                BloqueioTeclado.Desativar();

                WindowState =
                    WindowState.Normal;

                WindowStyle =
                    WindowStyle.SingleBorderWindow;

                ResizeMode =
                    ResizeMode.CanResize;

                Topmost =
                    false;
            }
            else
            {
                WindowState =
                    WindowState.Maximized;

                WindowStyle =
                    WindowStyle.None;

                ResizeMode =
                    ResizeMode.NoResize;

                Topmost =
                    true;

                BloqueioTeclado.Ativar();
            }

            TempoBloqueadoText.Text =
                "TEMPO: 00:00:00";

            Activate();
            Focus();
        }

        private void Window_Closing(
            object? sender,
            System.ComponentModel.CancelEventArgs e)
        {
            if (
                estado == EstadoComputador.Bloqueado &&
                !ConfiguracaoCliente.ModoDesenvolvedor
            )
            {
                e.Cancel = true;

                Activate();

                return;
            }

            BloqueioTeclado.Desativar();

            timerExibicao.Stop();

            timerHeartbeat.Stop();

            LimparConexaoAnterior();
        }
    }
}
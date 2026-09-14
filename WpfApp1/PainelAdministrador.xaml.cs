using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfApp1.Core;

namespace WpfApp1
{
    public partial class PainelAdministrador : Window
    {
        private readonly string ipServidor =
            ConfiguracaoRede.LerValor(
                System.IO.Path.Combine(AppContext.BaseDirectory, "painel.config"),
                "ServidorIp",
                "127.0.0.1"
            );

        private const int PortaServidor =
            5000;

        private static readonly TimeSpan IntervaloReconexao =
            TimeSpan.FromSeconds(5);

        private TcpClient? cliente;
        private NetworkStream? stream;
        private StreamReader? leitor;
        private StreamWriter? escritor;

        private HistoricoWindow? janelaHistorico;

        private readonly string[] nomesComputadores =
        {
            "PC 01",
            "PC 02",
            "PC 03",
            "PC 04",
            "PC 05"
        };

        private class StatusComputador
        {
            public string Nome { get; set; } = "";

            public EstadoComputador Estado { get; set; } =
                EstadoComputador.Disponivel;

            public int SegundosRestantes { get; set; }

            public bool ExtensaoSolicitada { get; set; }

            public bool Online { get; set; }
        }

        private readonly Dictionary<string, StatusComputador> estados = new();

        public PainelAdministrador()
        {
            InitializeComponent();

            foreach (string nome in nomesComputadores)
            {
                estados[nome] =
                    new StatusComputador
                    {
                        Nome = nome
                    };
            }

            MostrarComputadores();

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

                AtualizarStatusConexao(
                    "Reconectando...",
                    "CorAviso"
                );

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

                await escritor.WriteLineAsync(
                    "REGISTRAR_ADMIN|ADMIN"
                );

                AtualizarStatusConexao(
                    "Conectado ao servidor",
                    "CorEmUso"
                );

                return true;
            }
            catch
            {
                AtualizarStatusConexao(
                    "Servidor indisponível — tentando novamente...",
                    "CorBloqueado"
                );

                return false;
            }
        }

        private void AtualizarStatusConexao(
            string texto,
            string corChave)
        {
            Dispatcher.Invoke(() =>
            {
                StatusConexaoText.Text =
                    texto;

                StatusConexaoText.Foreground =
                    (Brush)Application.Current.Resources[corChave];
            });
        }

        private async Task EnviarMensagemAsync(
            string mensagem)
        {
            if (escritor == null)
                return;

            try
            {
                await escritor.WriteLineAsync(
                    mensagem
                );
            }
            catch
            {
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
                        mensagem
                    );
                }
            }
            catch
            {
            }

            AtualizarStatusConexao(
                "Conexão perdida — tentando novamente...",
                "CorBloqueado"
            );
        }

        private void ProcessarMensagem(
            string mensagem)
        {
            if (mensagem.StartsWith("HISTORICO|"))
            {
                string json =
                    mensagem["HISTORICO|".Length..];

                try
                {
                    List<RegistroSessao>? registros =
                        JsonSerializer.Deserialize<List<RegistroSessao>>(json);

                    if (registros != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (janelaHistorico == null)
                            {
                                janelaHistorico =
                                    new HistoricoWindow
                                    {
                                        Owner = this
                                    };

                                janelaHistorico.Closed +=
                                    (s, ev) => janelaHistorico = null;
                            }

                            janelaHistorico.CarregarRegistros(registros);

                            if (!janelaHistorico.IsVisible)
                            {
                                janelaHistorico.Show();
                            }
                            else
                            {
                                janelaHistorico.Activate();
                            }
                        });
                    }
                }
                catch
                {
                }

                return;
            }

            string[] partes =
                mensagem.Split('|');

            if (partes.Length < 7)
                return;

            if (partes[0] != "ESTADO")
                return;

            string nome =
                partes[1];

            if (
                !estados.TryGetValue(
                    nome,
                    out StatusComputador? computador
                )
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

            computador.Estado =
                novoEstado;

            if (
                int.TryParse(
                    partes[3],
                    out int segundos
                )
            )
            {
                computador.SegundosRestantes =
                    segundos;
            }

            if (
                bool.TryParse(
                    partes[4],
                    out bool extensao
                )
            )
            {
                computador.ExtensaoSolicitada =
                    extensao;
            }

            if (
                bool.TryParse(
                    partes[6],
                    out bool online
                )
            )
            {
                computador.Online =
                    online;
            }

            Dispatcher.Invoke(
                MostrarComputadores
            );
        }

        private async void BotaoAcao_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (
                sender is not Button botao ||
                botao.Tag is not string nome
            )
            {
                return;
            }

            if (
                !estados.TryGetValue(
                    nome,
                    out StatusComputador? computador
                )
            )
            {
                return;
            }

            if (!computador.Online)
            {
                return;
            }

            if (computador.Estado == EstadoComputador.EmUso)
            {
                await EnviarMensagemAsync(
                    $"ADMIN_ENCERRAR|{nome}"
                );
            }
            else
            {
                await EnviarMensagemAsync(
                    $"ADMIN_INICIAR|{nome}"
                );
            }
        }

        private async void BotaoConcederExtensao_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (
                sender is not Button botao ||
                botao.Tag is not string nome
            )
            {
                return;
            }

            await EnviarMensagemAsync(
                $"ADMIN_CONCEDER_EXTENSAO|{nome}"
            );
        }

        private async void BotaoRecusarExtensao_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (
                sender is not Button botao ||
                botao.Tag is not string nome
            )
            {
                return;
            }

            await EnviarMensagemAsync(
                $"ADMIN_RECUSAR_EXTENSAO|{nome}"
            );
        }

        private async void BotaoDesbloquear_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (
                sender is not Button botao ||
                botao.Tag is not string nome
            )
            {
                return;
            }

            await EnviarMensagemAsync(
                $"ADMIN_DESBLOQUEAR|{nome}"
            );
        }

        private async void BotaoHistorico_Click(
            object sender,
            RoutedEventArgs e)
        {
            await EnviarMensagemAsync(
                "ADMIN_HISTORICO"
            );
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

        private static Brush CorParaEstado(
            EstadoComputador estado,
            bool online)
        {
            if (!online)
            {
                return (Brush)Application.Current.Resources["CorOffline"];
            }

            return estado switch
            {
                EstadoComputador.EmUso =>
                    (Brush)Application.Current.Resources["CorEmUso"],

                EstadoComputador.Bloqueado =>
                    (Brush)Application.Current.Resources["CorBloqueado"],

                _ =>
                    (Brush)Application.Current.Resources["CorDisponivel"]
            };
        }

        private void MostrarComputadores()
        {
            ListaComputadores.Children.Clear();

            foreach (string nome in nomesComputadores)
            {
                StatusComputador computador = estados[nome];

                Brush corStatus =
                    CorParaEstado(computador.Estado, computador.Online);

                Border painel = new Border
                {
                    Background = (Brush)Application.Current.Resources["CorCard"],
                    BorderBrush = (Brush)Application.Current.Resources["CorBordaCard"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(18),
                    Margin = new Thickness(0, 0, 16, 16),
                    Width = 260
                };

                StackPanel conteudo = new StackPanel();

                Grid cabecalho = new Grid();

                cabecalho.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                );

                cabecalho.ColumnDefinitions.Add(
                    new ColumnDefinition { Width = GridLength.Auto }
                );

                TextBlock nomeText = new TextBlock
                {
                    Text = computador.Nome,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)Application.Current.Resources["CorTextoPrimario"]
                };

                Ellipse bolinha = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = corStatus,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetColumn(nomeText, 0);
                Grid.SetColumn(bolinha, 1);

                cabecalho.Children.Add(nomeText);
                cabecalho.Children.Add(bolinha);

                conteudo.Children.Add(cabecalho);

                TextBlock conexaoText = new TextBlock
                {
                    Text = computador.Online ? "Online" : "Offline",
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.Resources["CorTextoSecundario"],
                    Margin = new Thickness(0, 2, 0, 12)
                };

                conteudo.Children.Add(conexaoText);

                int minutos = computador.SegundosRestantes / 60;
                int segundos = computador.SegundosRestantes % 60;

                TextBlock tempoText = new TextBlock
                {
                    Text = $"{minutos:00}:{segundos:00}",
                    FontSize = 34,
                    FontWeight = FontWeights.Bold,
                    Foreground = corStatus,
                    FontFamily = new FontFamily("Consolas")
                };

                conteudo.Children.Add(tempoText);

                TextBlock estadoText = new TextBlock
                {
                    Text = TextoEstado(computador.Estado).ToUpper(),
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = corStatus,
                    Margin = new Thickness(0, 2, 0, 14)
                };

                conteudo.Children.Add(estadoText);

                if (computador.Online)
                {
                    Button botaoAcao = new Button
                    {
                        Content =
                            computador.Estado == EstadoComputador.EmUso
                                ? "ENCERRAR"
                                : "INICIAR",

                        Height = 38,
                        Margin = new Thickness(0, 0, 0, 8),
                        Tag = computador.Nome,

                        Style = (Style)Application.Current.Resources[
                            computador.Estado == EstadoComputador.EmUso
                                ? "EstiloBotaoPerigo"
                                : "EstiloBotaoPrimario"
                        ],

                        IsEnabled = computador.Estado != EstadoComputador.Bloqueado
                    };

                    botaoAcao.Click += BotaoAcao_Click;

                    conteudo.Children.Add(botaoAcao);
                }
                else
                {
                    TextBlock offlineText = new TextBlock
                    {
                        Text = "Computador não conectado.",
                        FontSize = 13,
                        Foreground = (Brush)Application.Current.Resources["CorTextoSecundario"],
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    conteudo.Children.Add(offlineText);
                }

                if (computador.ExtensaoSolicitada)
                {
                    TextBlock aviso = new TextBlock
                    {
                        Text = "⚠ Solicitação de +30 minutos",
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)Application.Current.Resources["CorAviso"],
                        Margin = new Thickness(0, 4, 0, 8),
                        TextWrapping = TextWrapping.Wrap
                    };

                    conteudo.Children.Add(aviso);

                    Button conceder = new Button
                    {
                        Content = "CONCEDER +30 MIN",
                        Height = 36,
                        Margin = new Thickness(0, 0, 0, 6),
                        Tag = computador.Nome,
                        Style = (Style)Application.Current.Resources["EstiloBotaoPrimario"]
                    };

                    conceder.Click += BotaoConcederExtensao_Click;

                    Button recusar = new Button
                    {
                        Content = "RECUSAR",
                        Height = 36,
                        Tag = computador.Nome,
                        Style = (Style)Application.Current.Resources["EstiloBotaoSecundario"]
                    };

                    recusar.Click += BotaoRecusarExtensao_Click;

                    conteudo.Children.Add(conceder);
                    conteudo.Children.Add(recusar);
                }

                if (computador.Estado == EstadoComputador.Bloqueado)
                {
                    Button botaoDesbloquear = new Button
                    {
                        Content = "🔒 DESBLOQUEAR",
                        Height = 38,
                        Margin = new Thickness(0, 4, 0, 0),
                        Tag = computador.Nome,
                        Style = (Style)Application.Current.Resources["EstiloBotaoAviso"],
                        IsEnabled = computador.Online
                    };

                    botaoDesbloquear.Click += BotaoDesbloquear_Click;

                    conteudo.Children.Add(botaoDesbloquear);
                }

                painel.Child = conteudo;

                ListaComputadores.Children.Add(painel);
            }
        }

        protected override void OnClosed(
            EventArgs e)
        {
            LimparConexaoAnterior();

            base.OnClosed(e);
        }
    }
}
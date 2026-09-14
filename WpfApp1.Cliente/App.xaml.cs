using System.Windows;

namespace WpfApp1.Cliente
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfiguracaoCliente.Carregar(e.Args);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Camada extra: garante que o hook nunca sobreviva
            // além da janela, mesmo em caminhos de saída atípicos.
            BloqueioTeclado.Desativar();

            base.OnExit(e);
        }
    }
}
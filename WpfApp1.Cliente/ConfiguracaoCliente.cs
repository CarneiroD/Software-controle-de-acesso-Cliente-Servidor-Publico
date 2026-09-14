using System;
using System.IO;

namespace WpfApp1.Cliente
{
    public static class ConfiguracaoCliente
    {
        private const string NomeArquivoConfig = "cliente.config";

        public static string NomeComputador { get; set; } = "PC 01";

        public static string IpServidor { get; set; } = "127.0.0.1";

        public static bool ModoDesenvolvedor { get; set; } = true;

        public static void Carregar(string[] args)
        {
            string caminho = Path.Combine(
                AppContext.BaseDirectory,
                NomeArquivoConfig
            );

            NomeComputador =
                WpfApp1.Core.ConfiguracaoRede.LerValor(
                    caminho,
                    "Nome",
                    NomeComputador
                );

            IpServidor =
                WpfApp1.Core.ConfiguracaoRede.LerValor(
                    caminho,
                    "ServidorIp",
                    IpServidor
                );

            string modoDesenvolvedorTexto =
                WpfApp1.Core.ConfiguracaoRede.LerValor(
                    caminho,
                    "ModoDesenvolvedor",
                    ModoDesenvolvedor.ToString()
                );

            if (
                bool.TryParse(
                    modoDesenvolvedorTexto,
                    out bool modoDesenvolvedorLido
                )
            )
            {
                ModoDesenvolvedor =
                    modoDesenvolvedorLido;
            }

            // Argumentos de linha de comando continuam tendo prioridade,
            // úteis pra simular vários PCs no seu ambiente de dev.
            if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            {
                NomeComputador = args[0].Trim();
            }

            if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
            {
                IpServidor = args[1].Trim();
            }
        }
    }
}
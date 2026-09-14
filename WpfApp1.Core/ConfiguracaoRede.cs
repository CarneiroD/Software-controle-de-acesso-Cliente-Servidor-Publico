using System;
using System.IO;

namespace WpfApp1.Core
{
    public static class ConfiguracaoRede
    {
        public static string LerValor(
            string caminhoArquivo,
            string chave,
            string valorPadrao)
        {
            if (!File.Exists(caminhoArquivo))
                return valorPadrao;

            foreach (string linha in File.ReadAllLines(caminhoArquivo))
            {
                string linhaLimpa = linha.Trim();

                if (linhaLimpa.Length == 0 || linhaLimpa.StartsWith("#"))
                    continue;

                int separador = linhaLimpa.IndexOf('=');

                if (separador <= 0)
                    continue;

                string chaveArquivo =
                    linhaLimpa[..separador].Trim();

                string valor =
                    linhaLimpa[(separador + 1)..].Trim();

                if (
                    string.Equals(
                        chaveArquivo,
                        chave,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return valor;
                }
            }

            return valorPadrao;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using WpfApp1.Core;

namespace WpfApp1
{
    public class RegistroSessaoExibicao
    {
        public string Computador { get; set; } = "";

        public string InicioFormatado { get; set; } = "";

        public string FimFormatado { get; set; } = "";

        public string DuracaoFormatada { get; set; } = "";

        public string MotivoFormatado { get; set; } = "";
    }

    public partial class HistoricoWindow : Window
    {
        public HistoricoWindow()
        {
            InitializeComponent();
        }

        public void CarregarRegistros(
            List<RegistroSessao> registros)
        {
            GradeHistorico.ItemsSource =
                registros
                    .Select(r => new RegistroSessaoExibicao
                    {
                        Computador = r.Computador,

                        InicioFormatado =
                            r.InicioUtc.ToLocalTime()
                                .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),

                        FimFormatado =
                            r.FimUtc.HasValue
                                ? r.FimUtc.Value.ToLocalTime()
                                    .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                                : "Em andamento",

                        DuracaoFormatada =
                            r.DuracaoSegundos.HasValue
                                ? TimeSpan.FromSeconds(r.DuracaoSegundos.Value)
                                    .ToString(@"hh\:mm\:ss")
                                : "-",

                        MotivoFormatado =
                            r.Motivo switch
                            {
                                "Manual" => "Encerrado pelo administrador",
                                "TempoEsgotado" => "Tempo esgotado",
                                _ => "-"
                            }
                    })
                    .ToList();
        }
    }
}
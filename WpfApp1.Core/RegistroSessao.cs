using System;

namespace WpfApp1.Core
{
    public class RegistroSessao
    {
        public int Id { get; set; }

        public string Computador { get; set; } = "";

        public DateTime InicioUtc { get; set; }

        public DateTime? FimUtc { get; set; }

        public int? DuracaoSegundos { get; set; }

        public string? Motivo { get; set; }
    }
}
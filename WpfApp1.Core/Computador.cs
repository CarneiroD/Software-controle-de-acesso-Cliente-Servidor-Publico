namespace WpfApp1.Core
{
    public class Computador
    {
        public string Nome { get; set; } = "";

        public int SegundosRestantes { get; set; }

        public int SegundosExtensao { get; set; }

        public EstadoComputador Estado { get; set; } = EstadoComputador.Disponivel;

        public bool ExtensaoSolicitada { get; set; }

        public bool Online { get; set; }
    }
}
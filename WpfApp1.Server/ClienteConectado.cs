using System.Net.Sockets;

namespace WpfApp1.Server
{
    public class ClienteConectado
    {
        public string NomeComputador { get; set; }

        public TcpClient Conexao { get; set; }

        public NetworkStream Stream { get; set; }

        public ClienteConectado(
            string nomeComputador,
            TcpClient conexao)
        {
            NomeComputador = nomeComputador;
            Conexao = conexao;
            Stream = conexao.GetStream();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace WpfApp1.Core
{
    public class SessaoIniciadaEventArgs : EventArgs
    {
        public string NomeComputador { get; }

        public SessaoIniciadaEventArgs(string nomeComputador)
        {
            NomeComputador = nomeComputador;
        }
    }

    public class SessaoEncerradaEventArgs : EventArgs
    {
        public string NomeComputador { get; }

        public string Motivo { get; }

        public SessaoEncerradaEventArgs(string nomeComputador, string motivo)
        {
            NomeComputador = nomeComputador;
            Motivo = motivo;
        }
    }

    public class GerenciadorSessoes
    {
        public List<Computador> Computadores { get; }

        public event EventHandler? SessoesAtualizadas;

        public event EventHandler<SessaoIniciadaEventArgs>? SessaoIniciada;

        public event EventHandler<SessaoEncerradaEventArgs>? SessaoEncerrada;

        public GerenciadorSessoes()
        {
            Computadores = new List<Computador>
            {
                CriarComputador("PC 01"),
                CriarComputador("PC 02"),
                CriarComputador("PC 03"),
                CriarComputador("PC 04"),
                CriarComputador("PC 05")
            };
        }

        private Computador CriarComputador(
            string nome)
        {
            return new Computador
            {
                Nome = nome,

                SegundosRestantes =
                    ConfiguracaoSistema.DuracaoSessaoSegundos,

                SegundosExtensao =
                    0,

                Estado =
                    EstadoComputador.Disponivel,

                ExtensaoSolicitada =
                    false,

                Online =
                    false
            };
        }

        public bool IniciarSessao(
            string nomeComputador)
        {
            Computador? computador =
                EncontrarComputador(
                    nomeComputador
                );

            if (computador == null)
                return false;

            if (!computador.Online)
                return false;

            if (computador.Estado == EstadoComputador.Bloqueado)
                return false;

            computador.SegundosRestantes =
                ConfiguracaoSistema.DuracaoSessaoSegundos;

            computador.SegundosExtensao =
                0;

            computador.ExtensaoSolicitada =
                false;

            computador.Estado =
                EstadoComputador.EmUso;

            NotificarAtualizacao();

            SessaoIniciada?.Invoke(
                this,
                new SessaoIniciadaEventArgs(nomeComputador)
            );

            return true;
        }

        public bool EncerrarSessao(
            string nomeComputador)
        {
            Computador? computador =
                EncontrarComputador(
                    nomeComputador
                );

            if (computador == null)
                return false;

            bool tinhaSessaoAtiva =
                computador.Estado == EstadoComputador.EmUso;

            computador.SegundosRestantes =
                0;

            computador.ExtensaoSolicitada =
                false;

            computador.Estado =
                EstadoComputador.Bloqueado;

            NotificarAtualizacao();

            if (tinhaSessaoAtiva)
            {
                SessaoEncerrada?.Invoke(
                    this,
                    new SessaoEncerradaEventArgs(nomeComputador, "Manual")
                );
            }

            return true;
        }

        public ResultadoSolicitacaoExtensao SolicitarExtensao(
            string nomeComputador)
        {
            Computador? computador =
                EncontrarComputador(
                    nomeComputador
                );

            if (computador == null)
                return ResultadoSolicitacaoExtensao.Indisponivel;

            if (!computador.Online)
                return ResultadoSolicitacaoExtensao.Indisponivel;

            if (computador.Estado != EstadoComputador.EmUso)
                return ResultadoSolicitacaoExtensao.Indisponivel;

            if (computador.ExtensaoSolicitada)
                return ResultadoSolicitacaoExtensao.JaPendente;

            if (
                computador.SegundosExtensao >=
                ConfiguracaoSistema.DuracaoExtensaoSegundos
            )
            {
                return ResultadoSolicitacaoExtensao.JaUtilizada;
            }

            computador.ExtensaoSolicitada =
                true;

            NotificarAtualizacao();

            return ResultadoSolicitacaoExtensao.Sucesso;
        }

        public bool ConcederExtensao(
            string nomeComputador)
        {
            Computador? computador =
                EncontrarComputador(
                    nomeComputador
                );

            if (computador == null)
                return false;

            if (!computador.Online)
                return false;

            if (computador.Estado != EstadoComputador.EmUso)
                return false;

            if (!computador.ExtensaoSolicitada)
                return false;

            if (
                computador.SegundosExtensao >=
                ConfiguracaoSistema.DuracaoExtensaoSegundos
            )
            {
                return false;
            }

            computador.SegundosRestantes +=
                ConfiguracaoSistema.DuracaoExtensaoSegundos;

            computador.SegundosExtensao +=
                ConfiguracaoSistema.DuracaoExtensaoSegundos;

            computador.ExtensaoSolicitada =
                false;

            NotificarAtualizacao();

            return true;
        }

        public bool RecusarExtensao(
            string nomeComputador)
        {
            Computador? computador =
                EncontrarComputador(
                    nomeComputador
                );

            if (computador == null)
                return false;

            computador.ExtensaoSolicitada =
                false;

            NotificarAtualizacao();

            return true;
        }

        public bool DesbloquearComputador(
            string nomeComputador)
        {
            Computador? computador =
                EncontrarComputador(
                    nomeComputador
                );

            if (computador == null)
                return false;

            if (computador.Estado != EstadoComputador.Bloqueado)
                return false;

            computador.Estado =
                EstadoComputador.Disponivel;

            computador.ExtensaoSolicitada =
                false;

            computador.SegundosRestantes =
                0;

            NotificarAtualizacao();

            return true;
        }

        public void AtualizarSessoes()
        {
            foreach (
                Computador computador
                in Computadores)
            {
                if (!computador.Online)
                    continue;

                if (computador.Estado != EstadoComputador.EmUso)
                    continue;

                if (computador.SegundosRestantes > 0)
                {
                    computador.SegundosRestantes--;
                }

                if (computador.SegundosRestantes <= 0)
                {
                    computador.SegundosRestantes =
                        0;

                    computador.Estado =
                        EstadoComputador.Bloqueado;

                    computador.ExtensaoSolicitada =
                        false;

                    SessaoEncerrada?.Invoke(
                        this,
                        new SessaoEncerradaEventArgs(computador.Nome, "TempoEsgotado")
                    );
                }
            }

            NotificarAtualizacao();
        }

        public Computador? ObterComputador(
            string nomeComputador)
        {
            return EncontrarComputador(
                nomeComputador
            );
        }

        private Computador? EncontrarComputador(
            string nomeComputador)
        {
            return Computadores.FirstOrDefault(
                computador =>
                    computador.Nome ==
                    nomeComputador
            );
        }

        private void NotificarAtualizacao()
        {
            SessoesAtualizadas?.Invoke(
                this,
                EventArgs.Empty
            );
        }
    }
}
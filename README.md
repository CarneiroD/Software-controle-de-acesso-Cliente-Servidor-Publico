# Sistema de Controle de Tempo e Acesso a Computadores

Sistema cliente-servidor desenvolvido em **C# e .NET 10** para gerenciamento centralizado do tempo de utilização de computadores em uma rede local.

O projeto foi desenvolvido com foco em ambientes que disponibilizam computadores para utilização controlada, permitindo ao administrador acompanhar o estado dos equipamentos e gerenciar as sessões através de um painel central.

## Funcionalidades

* Controle centralizado das sessões de utilização
* Sessões com tempo definido
* Contagem regressiva controlada pelo servidor
* Solicitação de extensão de tempo
* Aprovação ou recusa de extensões pelo administrador
* Bloqueio automático após o término da sessão
* Desbloqueio através do painel administrativo
* Identificação de computadores online e offline
* Pausa da sessão quando o computador cliente fica offline
* Comunicação cliente-servidor através de TCP/IP
* Heartbeat para monitoramento da conexão
* Painel administrativo centralizado
* Configuração individual dos computadores clientes

## Arquitetura

O sistema utiliza uma arquitetura cliente-servidor:

```text
                    Painel Administrativo
                             │
                             │ TCP/IP
                             ▼
                    ┌─────────────────┐
                    │     Server      │
                    │                 │
                    │ Controle das    │
                    │ sessões         │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
           Cliente        Cliente        Cliente
            PC 01          PC 02          PC 03
```

O servidor mantém o estado oficial das sessões e dos computadores. Os clientes enviam comandos e recebem atualizações do servidor.

## Projetos da solução

### WpfApp1.Core

Biblioteca compartilhada responsável pelas regras de negócio e pelo gerenciamento das sessões.

### WpfApp1.Server

Servidor central responsável pela comunicação TCP/IP e pelo controle das sessões.

### WpfApp1.Cliente

Aplicação instalada nos computadores utilizados pelos usuários.

### WpfApp1

Aplicação responsável pelo painel administrativo.

## Tecnologias

* C#
* .NET 10
* WPF
* TCP/IP
* TcpClient / TcpListener
* Arquitetura cliente-servidor
* SQLite — planejado para persistência dos dados

## Estado do projeto

O sistema encontra-se em desenvolvimento.

### Implementado

* [x] Gerenciamento de sessões
* [x] Comunicação TCP/IP
* [x] Painel administrativo
* [x] Controle de tempo
* [x] Extensão de sessão
* [x] Bloqueio automático
* [x] Desbloqueio administrativo
* [x] Monitoramento online/offline
* [x] Heartbeat
* [x] Reconexão do cliente
* [x] Modo seguro para desenvolvimento
* [x] Persistência com banco de dados
* [x] Configuração completa de rede
* [x] Histórico de utilização
* [x] Sistema de instalação
* [x] Configuração para múltiplos computadores
* [x] Bloqueio integrado ao ambiente Windows para produção

### Em desenvolvimento

* [ ] Autenticação administrativa

## Objetivo

O projeto tem como objetivo fornecer uma solução simples e centralizada para gerenciamento de computadores disponibilizados para utilização controlada em ambientes institucionais.

A arquitetura foi projetada para permitir a expansão futura do sistema para outros ambientes e quantidades de computadores.

## Status

Projeto em desenvolvimento para fins de estudo, portfólio e futura utilização em ambiente real.

> **Nota:** funcionalidades de produção relacionadas ao bloqueio do sistema operacional e segurança administrativa ainda estão em desenvolvimento e não devem ser utilizadas em ambiente produtivo.

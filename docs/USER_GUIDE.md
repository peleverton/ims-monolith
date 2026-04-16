# 📘 Guia do Usuário — IMS (Issue & Inventory Management System)

> **Versão:** 1.0 • **Data:** Abril 2026

---

## Sumário

1. [O que é o IMS?](#o-que-é-o-ims)
2. [Como Acessar](#como-acessar)
3. [Primeiro Acesso](#primeiro-acesso)
4. [Navegação Geral](#navegação-geral)
5. [Módulo: Issues](#módulo-issues)
6. [Módulo: Estoque (Inventory)](#módulo-estoque-inventory)
7. [Módulo: Analytics](#módulo-analytics)
8. [Notificações em Tempo Real](#notificações-em-tempo-real)
9. [Gerenciamento de Usuários (Admin)](#gerenciamento-de-usuários-admin)
10. [Tema Claro / Escuro](#tema-claro--escuro)
11. [Idioma](#idioma)
12. [Perguntas Frequentes](#perguntas-frequentes)

---

## O que é o IMS?

O **IMS** é um sistema de gestão de chamados e inventário. Com ele você pode:

- Registrar e acompanhar **issues** (problemas, tarefas, solicitações)
- Controlar o **estoque** de produtos, fornecedores e localizações
- Visualizar **indicadores e relatórios** em tempo real
- Receber **notificações** instantâneas sobre eventos importantes
- Gerenciar **usuários e permissões** (perfil Administrador)

---

## Como Acessar

Se você está rodando o sistema localmente, basta executar:

```bash
docker compose up -d
```

E acessar no navegador: **http://localhost:3000**

> Precisa ter o [Docker](https://www.docker.com/get-started) instalado. Peça ao time técnico para configurar o ambiente na primeira vez.

Para o ambiente de produção, acesse:

```
https://app.ims.com
```

O sistema funciona nos navegadores modernos: **Chrome**, **Firefox**, **Edge** e **Safari**.

---

## Primeiro Acesso

### Criando uma conta

1. Na tela inicial, clique em **"Criar conta"**
2. Preencha:
   - **Nome de usuário** — sem espaços, mínimo 3 caracteres
   - **E-mail** — endereço de e-mail válido
   - **Senha** — mínimo 6 caracteres
3. Clique em **"Registrar"**
4. Você será redirecionado automaticamente para o painel

### Fazendo login

1. Acesse a página inicial
2. Informe seu **e-mail** e **senha**
3. Clique em **"Entrar"**
4. A sessão é mantida automaticamente — o token de acesso é renovado em segundo plano

### Esqueci minha senha

Entre em contato com o administrador do sistema para redefinição de senha.

---

## Navegação Geral

Após o login você verá o **painel principal** com:

```
┌──────────────────────────────────────────────────────┐
│  🔔 [sino]  🌙 [tema]  👤 [perfil]    [Sair]        │
├──────────────────────────────────────────────────────┤
│  🏠 Dashboard                                        │
│  📋 Issues                                           │
│  📦 Estoque                                          │
│  📊 Analytics                                        │
│  ⚙️  Admin (somente admins)                          │
└──────────────────────────────────────────────────────┘
```

| Ícone | Função |
|---|---|
| 🔔 Sino | Notificações recentes — clique para ver as últimas 10 |
| 🌙 Lua | Alternar entre tema claro e escuro |
| 👤 Perfil | Ver seu perfil e fazer logout |

---

## Módulo: Issues

### O que é uma Issue?

Uma **issue** representa um chamado, tarefa, problema ou solicitação que precisa ser acompanhado.

### Listagem de Issues

- Acesse **Issues** no menu lateral
- A lista exibe todas as issues com: título, status, prioridade, responsável e data de criação
- Use os **filtros** para buscar por status, prioridade ou período

### Status das Issues

| Status | Significado |
|---|---|
| `Open` | Aberta, aguardando ação |
| `InProgress` | Em andamento |
| `Resolved` | Resolvida |
| `Closed` | Encerrada |

### Criando uma Issue

1. Clique em **"Nova Issue"**
2. Preencha:
   - **Título** — descrição curta e objetiva
   - **Descrição** — detalhes do problema
   - **Prioridade** — Low, Medium, High, Critical
3. Clique em **"Criar"**

### Atualizando uma Issue

- Clique sobre uma issue para abrir o detalhe
- Você pode alterar o **status**, adicionar **comentários** e ver o histórico de atividades

---

## Módulo: Estoque (Inventory)

### Produtos

- Acesse **Estoque > Produtos**
- Visualize todos os produtos cadastrados com quantidade, preço e status
- Clique em **"Novo Produto"** para cadastrar
- Use os formulários para editar ou excluir

### Fornecedores

- Acesse **Estoque > Fornecedores**
- Gerencie os fornecedores vinculados aos produtos

### Localizações

- Acesse **Estoque > Localizações**
- Registre armazéns, prateleiras e posições de estoque

### Movimentações

- Toda alteração de quantidade gera um **registro de movimentação**
- Acesse o histórico pelo detalhe do produto

### Alerta de Estoque Baixo

Quando um produto atinge o **estoque mínimo**, você receberá uma notificação automática em tempo real.

---

## Módulo: Analytics

O módulo de Analytics exibe indicadores consolidados do sistema:

| Indicador | Descrição |
|---|---|
| **Issues por Status** | Distribuição das issues abertas, em andamento e resolvidas |
| **Tendência de Issues** | Evolução ao longo do tempo |
| **Tempo de Resolução** | Média de tempo para fechar uma issue |
| **Valor de Estoque** | Valor total dos produtos em estoque |
| **Giro de Estoque** | Taxa de movimentação dos produtos |
| **Carga por Usuário** | Distribuição de issues por responsável |

> Os gráficos são atualizados automaticamente. Use o **painel Blazor** para visualizações avançadas.

---

## Notificações em Tempo Real

O IMS envia notificações automáticas para eventos importantes:

| Evento | Notificação |
|---|---|
| Nova issue criada | "Nova issue: [título]" |
| Issue resolvida | "Issue resolvida: [título]" |
| Estoque abaixo do mínimo | "Estoque crítico: [produto]" |

### Como funcionam

- O **sino** (🔔) no topo da página mostra a contagem de notificações não lidas
- Clique no sino para abrir o painel com as últimas 10 notificações
- As notificações são marcadas como lidas ao abrir o painel
- Um **toast** (mensagem flutuante) aparece no canto da tela a cada novo evento

---

## Gerenciamento de Usuários (Admin)

> ⚠️ Esta seção é visível apenas para usuários com perfil **Admin**.

### Acessando

- Clique em **Admin > Usuários** no menu lateral

### O que você pode fazer

| Ação | Como |
|---|---|
| **Ver todos os usuários** | Lista completa com nome, e-mail, role e status |
| **Convidar usuário** | Botão "Convidar" — informe e-mail e role |
| **Alterar role** | Botão de edição na linha do usuário |
| **Ativar / Desativar** | Toggle na coluna "Ativo" |

### Roles disponíveis

| Role | Permissões |
|---|---|
| `User` | Acesso padrão — ver e criar issues e estoque |
| `Admin` | Acesso completo + gerenciamento de usuários |

---

## Tema Claro / Escuro

- Clique no ícone 🌙 (lua) no topo da página para alternar o tema
- O sistema salva sua preferência automaticamente
- Também respeita a configuração do seu sistema operacional

---

## Idioma

O sistema suporta dois idiomas:

- 🇧🇷 **Português (PT-BR)** — padrão
- 🇺🇸 **English (EN-US)**

A troca de idioma é detectada automaticamente pelo navegador. Para forçar um idioma específico, acesse:

```
http://localhost:3000/pt     → Português
http://localhost:3000/en     → English
```

---

## Perguntas Frequentes

**❓ Esqueci minha senha, o que faço?**
Entre em contato com o administrador do sistema.

**❓ Não estou recebendo notificações.**
Verifique se você está conectado à internet. O sistema reconecta automaticamente em caso de queda, mas pode levar alguns segundos.

**❓ Não consigo ver o menu "Admin".**
Somente usuários com role `Admin` têm acesso. Solicite ao administrador que ajuste seu perfil.

**❓ O painel de analytics está vazio.**
Os dados são carregados a partir das issues e do estoque cadastrados. Se ainda não há dados, os gráficos estarão vazios.

**❓ Como exportar relatórios?**
Acesse **Analytics** e clique em **"Exportar"** — disponível nos formatos JSON, CSV e PDF.

---

*Dúvidas ou problemas? Contate o suporte interno ou abra uma issue no repositório.*

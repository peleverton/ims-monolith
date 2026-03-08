---
name: "ims-modular-patterns"
description: "Convenções do projeto, padrões de arquitetura e referência cruzada para todas as skills do projeto IMS Modular (.NET)"
domain: "project-conventions"
confidence: "high"
source: "manual"
---

## Context

Estas convenções aplicam-se a todo o trabalho no projeto **IMS Modular** — um monólito modular .NET baseado em Clean Architecture com Minimal API, CQRS (MediatR), EF Core e FluentValidation. Antes de modificar qualquer código, consulte as skills relevantes listadas abaixo.

## 📚 Skills Disponíveis

As skills abaixo estão localizadas em `skills/` e devem ser consultadas conforme o contexto da tarefa.

### 🏗️ Arquitetura & Estrutura

| Skill | Descrição | Quando usar |
|-------|-----------|-------------|
| [architecture-overview](skills/architecture-overview/SKILL.md) | Visão geral da arquitetura Clean Architecture do monólito modular .NET 9 | Entender Result Pattern, padrões de design, estrutura de pastas ou convenções de nomenclatura |
| [api-project-patterns](skills/api-project-patterns/SKILL.md) | Padrões do projeto Api: bootstrap, middleware, DI, Swagger, configuração | Modificar Program.cs, configurar ambientes ou adicionar novas configurações |
| [core-project-patterns](skills/core-project-patterns/SKILL.md) | Padrões do projeto Core: services, abstractions, domain models, validators e error mapping | Criar lógica de negócio, interfaces, DTOs, validators FluentValidation ou registrar dependências no DI |
| [minimal-api-modules](skills/minimal-api-modules/SKILL.md) | Criação e organização de módulos de Minimal API por domínio de negócio | Criar novos endpoints, implementar validação com FluentValidation, configurar autorização ou organizar rotas com MapGroup |

### 🔧 Implementação & Integração

| Skill | Descrição | Quando usar |
|-------|-----------|-------------|
| [error-mapping](skills/error-mapping/SKILL.md) | Mapeamento de erros por domínio usando ApplicationError e Railway-Oriented Programming | Criar novos domínios, definir enums de erro, mapear erros HTTP (400/422/500) |
| [source-generators-aot](skills/source-generators-aot/SKILL.md) | Configuração de Native AOT e Source Generators para serialização JSON sem reflection | Adicionar novos types para serialização, configurar JsonSerializerContext |
| [infrastructure-integrations](skills/infrastructure-integrations/SKILL.md) | Integrações com APIs externas, cache Redis e mapeamento de models | Criar integrations com HttpClient, implementar cache, mapear models externos |
| [code-templates](skills/code-templates/SKILL.md) | Templates copy-paste para scaffolding rápido de novos componentes | Criar um novo domínio completo, módulo de API, service, integration, validator ou testes |

### 📡 Mensageria (Event Hub / Kafka)

| Skill | Descrição | Quando usar |
|-------|-----------|-------------|
| [eventhub-producer](skills/eventhub-producer/SKILL.md) | Guia para criar producers de mensageria com Azure Event Hub / Kafka (Confluent) | Implementar um novo producer de eventos, incluindo DTO, abstração, registro no DI e serialização AOT |
| [eventhub-consumer](skills/eventhub-consumer/SKILL.md) | Guia para criar consumers de mensageria com Azure Event Hub / Kafka (Confluent) | Implementar um novo consumer de eventos, processamento idempotente, configuração de grupo e DLQ |

### 📊 Observabilidade & Qualidade

| Skill | Descrição | Quando usar |
|-------|-----------|-------------|
| [observability](skills/observability/SKILL.md) | Padrões de logging estruturado e business events com Dynatrace e OpenTelemetry | Implementar LogInformation/LogError, criar BusinessEvents ou definir metadata de eventos |
| [testing-patterns](skills/testing-patterns/SKILL.md) | Padrões de testes unitários com xUnit e Moq para services e integrations | Escrever testes de Service, Integration, validators ou criar Result\<T\> em mocks |

### 🛡️ Segurança & Code Quality (SonarQube)

| Skill | Descrição | Quando usar |
|-------|-----------|-------------|
| [code-smells](skills/code-smells/SKILL.md) | Catálogo de code smells e práticas não recomendadas detectadas pelo SonarQube | **SEMPRE** antes de gerar qualquer código .NET, para evitar práticas proibidas |
| [critical-bugs](skills/critical-bugs/SKILL.md) | Catálogo de bugs críticos que causam falhas em runtime | Revisar código existente, corrigir bugs ou validar implementações antes de merge |
| [security-vulnerabilities](skills/security-vulnerabilities/SKILL.md) | Vulnerabilidades de segurança críticas (OWASP, criptografia, SQL Injection, exposição de dados) | Implementar autenticação, dados sensíveis, queries ou comunicação externa |

## 🗺️ Fluxo de Consulta Recomendado

```
Nova feature/domínio:
  1. architecture-overview → entender a estrutura
  2. code-templates → scaffolding do domínio
  3. core-project-patterns → lógica de negócio
  4. minimal-api-modules → endpoints
  5. error-mapping → tratamento de erros
  6. testing-patterns → testes unitários
  7. code-smells + critical-bugs + security-vulnerabilities → validação final

Nova integração externa:
  1. infrastructure-integrations → HttpClient + cache
  2. source-generators-aot → serialização AOT
  3. observability → logging + tracing

Mensageria:
  1. eventhub-producer ou eventhub-consumer → scaffolding
  2. source-generators-aot → serialização de payloads
  3. observability → monitoramento
```

## Padrões Core do Projeto

### Module Structure
Every module follows the same 4-layer structure:
```
Modules/{ModuleName}/
├── Api/                    # Minimal API endpoint definitions
│   └── {Module}Module.cs   # Static Map(app) method for endpoints
├── Application/            # CQRS handlers, DTOs, validators
│   ├── Commands/           # IRequest<T> commands + handlers
│   ├── Queries/            # IRequest<T> queries + handlers
│   └── Validators/         # FluentValidation AbstractValidator<T>
├── Domain/                 # Entities, value objects, domain events
│   ├── Entities/
│   └── ValueObjects/
├── Infrastructure/         # EF Core DbContext, configurations, repos
│   ├── Data/
│   └── Repositories/
└── {Module}ModuleExtensions.cs  # AddXxxModule() DI registration
```

### Module Registration (Program.cs)
```csharp
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddIssuesModule(builder.Configuration);
AuthModule.Map(app);
IssuesModule.Map(app);
await app.Services.InitializeAuthModuleAsync();
await app.Services.InitializeIssuesModuleAsync();
```

### Endpoints

#### Auth
| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/auth/register` | Registrar novo usuário |
| POST | `/api/auth/login` | Login (retorna JWT) |

#### Issues (requer autenticação)
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/issues` | Listar issues (paginado) |
| GET | `/api/issues/{id}` | Buscar issue por ID |
| POST | `/api/issues` | Criar issue |
| PUT | `/api/issues/{id}` | Atualizar issue |
| PUT | `/api/issues/{id}/status` | Atualizar status |
| POST | `/api/issues/{id}/comments` | Adicionar comentário |
| DELETE | `/api/issues/{id}` | Deletar issue |

#### Health
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/ping` | Health check (pong) |
| GET | `/api/status` | Status do serviço |

### Seed Data
- **Roles**: Admin, User
- **Admin User**: `admin@ims.com` / `Admin@123`

### Adding a New Module
1. Create `Modules/{Name}/` with all 4 layers
2. Create `{Name}ModuleExtensions.cs` with `Add{Name}Module()` and `Initialize{Name}ModuleAsync()`
3. Register in `Program.cs`: `builder.Services.Add{Name}Module(builder.Configuration)`
4. Map endpoints: `{Name}Module.Map(app)`
5. Initialize: `await app.Services.Initialize{Name}ModuleAsync()`

## Anti-Patterns
- **Ignorar skills antes de codificar** — Sempre consulte as skills relevantes antes de iniciar qualquer implementação
- **Gerar código sem checar code-smells** — A skill `code-smells` deve ser consultada SEMPRE antes de gerar código .NET
- **Implementar segurança sem consultar security-vulnerabilities** — Qualquer funcionalidade envolvendo dados sensíveis deve passar pela skill de segurança
- **Criar domínio sem seguir templates** — Use `code-templates` para manter consistência no scaffolding
- **Leaking module internals** — modules should only expose their API layer. Never reference another module's Domain or Infrastructure
- **Fat Shared kernel** — only put things in Shared/ if 2+ modules genuinely need them
- **Skipping FluentValidation** — every command/query that takes user input must have a validator
- **Concrete infrastructure in handlers** — handlers depend on interfaces, Infrastructure implements them

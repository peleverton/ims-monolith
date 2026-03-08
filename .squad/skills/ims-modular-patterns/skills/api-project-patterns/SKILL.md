---
name: api-project-patterns
description: Padrões do projeto Api: bootstrap com CreateSlimBuilder, middleware pipeline, DI, Swagger, cache Redis, telemetria OpenTelemetry e configuração de variáveis de ambiente. Use ao modificar Program.cs, DependencyInjection.cs, configurar ambientes ou adicionar novas propriedades no EnvironmentConfiguration.
---

# 02. API Project Patterns

## 📋 Visão Geral

O projeto **Api** é a camada de apresentação do microsserviço, responsável por:

- Expor endpoints HTTP via Minimal API
- Validação de entrada (headers, body)
- Autorização e autenticação
- Configuração de middleware
- Registro de dependências
- Telemetria e observabilidade
- Compilação AOT (Ahead-of-Time)

**Arquivos Principais**:
- `Program.cs` - Bootstrap da aplicação
- `DependencyInjection.cs` - Registro de serviços

---

## 🚀 Bootstrap da Aplicação

### Program.cs - Estrutura

```csharp
// 1. WebApplication.CreateSlimBuilder (AOT-compatible)
var builder = WebApplication.CreateSlimBuilder(args);

// 2. Configuração de Cultura (pt-BR)
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 3. Logging JSON estruturado
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = false;
    options.UseUtcTimestamp = true;
    options.JsonWriterOptions = new JsonWriterOptions
    {
        Indented = builder.Environment.IsDevelopment()
    };
});

// 4. Cadeia de registro de dependências
builder.Services
    .AddApi()           // API layer
    .AddCore()          // Core layer
    .AddInfrastructure(); // Infrastructure layer

var app = builder.Build();

// 5. Middleware pipeline
app.UseExceptionHandler();
app.MapHealthChecks("/health");
app.UseHttpsRedirection();
app.UseSecurityHandler(options => { /* JWT config */ });

if (app.Environment.EnvironmentName == "Local")
    app.UseSwagger().UseSwaggerUI();

app.RegisterEndpoints(); // Source-generated

app.Run();
```

### Características do Bootstrap

#### **1. CreateSlimBuilder vs CreateBuilder**

```csharp
// ✅ Slim Builder - AOT compatible, minimal services
var builder = WebApplication.CreateSlimBuilder(args);

// ❌ Standard Builder - Reflection-heavy, mais serviços por padrão
// var builder = WebApplication.CreateBuilder(args);
```

**Vantagens Slim Builder**:
- Menos serviços registrados por padrão
- Compatível com Native AOT
- Startup mais rápido (~40% reduction)
- Menor footprint de memória

#### **2. Cultura Globalizada**

```csharp
var cultureInfo = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
```

**Impacto**: Datas `dd/MM/yyyy`, números `1.234,56`, moeda `R$ 1.234,56`.

---

## 🔧 Dependency Injection

### Cadeia de Registro

```csharp
public static IServiceCollection AddApi(this IServiceCollection services)
{
    return services
        .AddGlobalExceptionHandling()
        .AddEndpoints()
        .AddSwagger()
        .AddEnvironmentConfigurations()
        .AddCache()
        .AddHttpContextAccessorLocalDebug()
        .AddAppTelemetry();
}
```

### 1. Global Exception Handling

```csharp
private static IServiceCollection AddGlobalExceptionHandling(
    this IServiceCollection services)
{
    services.AddExceptionHandler<GlobalExceptionHandler>();
    services.AddProblemDetails();
    return services;
}
```

### 2. Environment Configurations

```csharp
private static IServiceCollection AddEnvironmentConfigurations(
    this IServiceCollection services)
{
    var config = new EnvironmentConfiguration
    {
        ContextEnvironment = Environment.GetEnvironmentVariable("CONTEXT_ENVIRONMENT"),
        UrlOrdersApi = Environment.GetEnvironmentVariable("URL_ORDERS_API"),
        UrlProductsApi = Environment.GetEnvironmentVariable("URL_PRODUCTS_API"),
        // ... outras URLs
    };
    
    services.AddSingleton(config);
    return services;
}
```

**Variáveis de Ambiente Requeridas**:
```bash
CONTEXT_ENVIRONMENT=Local|Dev|Hml|Prd
URL_ORDERS_API=https://api.example.com
REDIS_CACHE_CONNECTION=redis-server:6379
REDIS_CACHE_PASSWORD=****
DYNATRACE_OTLP_ENDPOINT=https://dynatrace.example.com
```

### 3. Redis Cache

```csharp
private static IServiceCollection AddCache(this IServiceCollection services)
{
    var redisConnection = Environment.GetEnvironmentVariable("REDIS_CACHE_CONNECTION");
    var redisPassword = Environment.GetEnvironmentVariable("REDIS_CACHE_PASSWORD");
    
    var connectionString = $"{redisConnection},password={redisPassword}";
    
    var redis = ConnectionMultiplexer.Connect(connectionString);
    services.AddSingleton<IConnectionMultiplexer>(redis);
    services.AddSingleton<IDatabase>(sp => 
        sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    services.AddScoped<IReadWriteCacheCommands, ReadWriteCacheCommands>();
    
    return services;
}
```

### 4. Application Telemetry

```csharp
private static IServiceCollection AddAppTelemetry(
    this IServiceCollection services,
    IHostEnvironment environment)
{
    if (environment.IsProduction())
    {
        var otlpEndpoint = Environment.GetEnvironmentVariable("DYNATRACE_OTLP_ENDPOINT");
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("microservice-{domain}"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)));
    }
    
    return services;
}
```

**⚠️ Apenas Produção**: Telemetria desabilitada em dev/local para performance.

---

## 🔐 Middleware Pipeline

### Ordem de Execução

```
Client → ExceptionHandler → HealthCheck → HttpsRedirection → SecurityHandler → [Swagger (Local)] → Endpoints
```

### Componentes

| Middleware | Responsabilidade |
|------------|-----------------|
| `UseExceptionHandler()` | Captura exceptions não tratadas → `ProblemDetails` RFC 7807 |
| `MapHealthChecks("/health")` | Kubernetes liveness/readiness probes |
| `UseHttpsRedirection()` | HTTP → HTTPS (301 Permanent Redirect) |
| `UseSecurityHandler()` | Validação JWT + descriptografia de headers |
| `UseSwagger()` | Swagger UI — **apenas ambiente Local** |
| `RegisterEndpoints()` | Source-generated, registra todos os módulos |

---

## ⚙️ Configurações por Ambiente

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**⚠️ Importante**: **NÃO** armazene secrets em appsettings.json. Use variáveis de ambiente.

### launchSettings.json

```json
{
  "profiles": {
    "Api": {
      "commandName": "Project",
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Local",
        "CONTEXT_ENVIRONMENT": "Local"
      }
    }
  }
}
```

---

## 🔐 Guia Completo: Adicionar Nova Configuração

Sempre que uma nova propriedade for criada no `EnvironmentConfiguration`, seguir este processo completo:

### 📋 Processo Passo a Passo

#### 1️⃣ Adicionar Propriedade no EnvironmentConfiguration

```csharp
public class EnvironmentConfiguration
{
    // Propriedades existentes...
    
    // ✅ Nova propriedade adicionada
    public string? BizEventUrl { get; set; }
    public string? BizEventToken { get; set; }
}
```

#### 2️⃣ Preencher no Dependency Injection

```csharp
private static IServiceCollection AddEnvironmentConfigurations(
    this IServiceCollection services)
{
    var config = new EnvironmentConfiguration
    {
        // Configurações existentes...
        
        // TODO: [CONFIG] Adicionar BIZ_EVENT_URL e BIZ_EVENT_TOKEN no:
        //       - launchSettings.json (ambiente Local)
        //       - Vault path: sofisa/dynatrace/biz_event (credenciais)
        //       - helm/values.yaml annotations (mapeamento Vault)
        BizEventUrl = Environment.GetEnvironmentVariable("BIZ_EVENT_URL"),
        BizEventToken = Environment.GetEnvironmentVariable("BIZ_EVENT_TOKEN"),
    };
    
    services.AddSingleton(config);
    return services;
}
```

#### 3️⃣ Configurar Ambiente Local (launchSettings.json)

```json
{
  "environmentVariables": {
    "BIZ_EVENT_URL": "https://dev.dynatrace.com/api/v2/bizevents",
    "BIZ_EVENT_TOKEN": "dt0s01.XXXXXXXXXXXX.YYYYYYYYYYYYYYYY"
  }
}
```

#### 4️⃣ Configuração por Tipo de Informação

##### 🔒 **Credenciais/Secrets** → Vault + Helm Annotations

```yaml
# helm/values.yaml
deployment:
  annotations:
    vault.hashicorp.com/agent-inject-secret-biz_event_config: "sofisa/dynatrace/biz_event"
    vault.hashicorp.com/agent-inject-template-biz_event_config: |
      {{ with secret "sofisa/dynatrace/biz_event" -}}
      export BIZ_EVENT_URL="{{ .Data.data.apiUrl }}"
      export BIZ_EVENT_TOKEN="{{ .Data.data.apiToken }}"
      {{- end }}
```

**⚠️ CRÍTICO**: Os nomes em `export NOME_VAR` devem ser **EXATAMENTE** iguais ao `Environment.GetEnvironmentVariable("NOME_VAR")`.

##### 🌐 **URLs/Configurações Não Sensíveis** → ConfigMap

```yaml
# helm/values.yaml
configmap:
  data:
    URL_API_NEWSERVICE: "http://new-service.namespace.svc.cluster.local/api"

# helm/values-dev.yaml (sobrescreve por ambiente)
configmap:
  data:
    URL_API_NEWSERVICE: "http://new-service-dev.namespace.svc.cluster.local/api"
```

### ⚠️ Regras para Assistentes de IA

Quando um assistente de IA adicionar uma nova configuração, **SEMPRE**:

1. **Criar TODO no código** com tipo:
   - `[CONFIG-VAULT]` → Credencial/secret → Vault
   - `[CONFIG-CONFIGMAP]` → URL/config pública → ConfigMap
   - `[CONFIG-LOCAL]` → Configuração local → launchSettings.json

2. **Listar arquivos afetados**:
   - `app/src/Api/Properties/launchSettings.json`
   - `helm/values.yaml` (Vault annotations ou ConfigMap)
   - `helm/values-dev.yaml`, `values-hml.yaml`, `values-prd.yaml`

### 🎯 Checklist Final

- [ ] Propriedade adicionada em `EnvironmentConfiguration`
- [ ] `Environment.GetEnvironmentVariable()` no DependencyInjection
- [ ] TODOs criados com instruções claras
- [ ] Variável adicionada em `launchSettings.json`
- [ ] Vault secret criado (se credencial)
- [ ] Helm annotations configuradas (se credencial)
- [ ] ConfigMap configurado (se não-sensível)
- [ ] Ajustado em todos `values-{env}.yaml`

### ❌ Erros Comuns

| Erro | Solução |
|------|---------|
| Variável não exportada no Vault template | Verificar que `export NOME_VAR` é igual ao `GetEnvironmentVariable("NOME_VAR")` |
| ConfigMap não sobrescrito por ambiente | Criar entrada em `values-{env}.yaml` |
| Variável null em Local | Adicionar em `launchSettings.json` |
| Case-sensitive mismatch | Variáveis de ambiente são **case-sensitive** no Linux/Kubernetes |

---

## 📊 Source Generators (AOT)

### Configuração no .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

**Chaves**:
- `PublishAot=true`: Habilita compilação Native AOT
- `InvariantGlobalization=false`: Permite culturas específicas (pt-BR)

### Source Generator Helpers

Pasta: `SourceGeneratorHelper/`

**Arquivos**:
- `{Domain}SourceGenerator.cs` - Metadados de serialização
- `SourceGeneratorMappings.cs` - Registro de type resolvers
- `EndpointRegistrationExtensions.cs` - Auto-registro de módulos
- `EnumSchemaFilter.cs` - Swagger enum filter

Detalhes em: **`.github/skills/source-generators-aot/SKILL.md`**

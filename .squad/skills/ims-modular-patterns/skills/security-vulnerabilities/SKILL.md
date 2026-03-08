---
name: security-vulnerabilities
description: Catálogo de vulnerabilidades de segurança críticas (OWASP, criptografia, SQL Injection, exposição de dados). Use ao implementar qualquer funcionalidade que envolva autenticação, dados sensíveis, queries ou comunicação externa.
---

# 13. Vulnerabilidades de Segurança

## 📋 Visão Geral

Este documento cataloga **vulnerabilidades de segurança críticas** detectadas pelo SonarQube que podem expor a aplicação a **ataques maliciosos**.

> **Vulnerabilidades são BLOQUEADORAS CRÍTICAS de deploy.**
> **ZERO vulnerabilidades em produção - sem exceções.**

### Mapeamento OWASP Top 10

- **A2**: Broken Authentication
- **A3**: Sensitive Data Exposure
- **A4**: XML External Entities (XXE)
- **A6**: Security Misconfiguration
- **A8**: Insecure Deserialization
- **A9**: Using Components with Known Vulnerabilities

---

## 📊 Resumo de Vulnerabilidades

| Categoria | OWASP | Exemplos |
|-----------|-------|----------|
| 🔐 Criptografia | A3, A6 | DES/3DES weak cipher, chaves curtas, IV estático, ECB mode |
| 🔑 Autenticação e Hashing | A2, A3 | Senha DB fraca, LDAP sem auth, hash sem salt, MD5/SHA1 |
| 🔒 SSL/TLS | A3, A6 | Protocolos fracos, certificados não verificados |
| 🎫 JWT e Tokens | A3 | Algoritmo `none`, segredo fraco |
| 📦 Serialização | A8, A4 | Deserialização insegura, XXE |
| 📁 File System | A9 | Temp files inseguros |

---

## 🔐 1. Criptografia

### ❌ Algoritmos de Cifra Fracos (EVITAR)

```csharp
// 💥 DES - OBSOLETO, quebrável em minutos (56-bit key)
using var des = DES.Create();
des.Key = key;
using var encryptor = des.CreateEncryptor();

// 💥 3DES (TripleDES) - DEPRECATED desde 2017 (Sweet32 attack)
using var tripleDes = TripleDES.Create();
tripleDes.Mode = CipherMode.ECB; // 💥 ECB + 3DES = dupla vulnerabilidade!

// 💥 RC2 - OBSOLETO e VULNERÁVEL
using var rc2 = RC2.Create();
```

```csharp
// ✅ CORRETO - AES-256-CBC com IV aleatório
public byte[] EncryptData(byte[] data, byte[] key)
{
    using var aes = Aes.Create();
    aes.KeySize = 256;               // ✅ AES-256
    aes.Mode = CipherMode.CBC;       // ✅ CBC (não ECB)
    aes.Padding = PaddingMode.PKCS7; // ✅ PKCS7
    aes.Key = key;
    aes.GenerateIV();                // ✅ IV aleatório único por mensagem

    using var encryptor = aes.CreateEncryptor();
    var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

    // ✅ Retorna IV + ciphertext
    var result = new byte[16 + encrypted.Length];
    Buffer.BlockCopy(aes.IV, 0, result, 0, 16);
    Buffer.BlockCopy(encrypted, 0, result, 16, encrypted.Length);
    return result;
}

// ✅ MELHOR - AES-GCM (autenticação integrada)
public byte[] EncryptWithGCM(byte[] data, byte[] key)
{
    var nonce = new byte[12]; // 96-bit nonce
    var tag = new byte[16];
    var ciphertext = new byte[data.Length];

    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(nonce); // ✅ Nonce aleatório

    using var aesGcm = new AesGcm(key);
    aesGcm.Encrypt(nonce, data, ciphertext, tag);

    // ✅ Retorna nonce + tag + ciphertext
    var result = new byte[12 + 16 + ciphertext.Length];
    Buffer.BlockCopy(nonce, 0, result, 0, 12);
    Buffer.BlockCopy(tag, 0, result, 12, 16);
    Buffer.BlockCopy(ciphertext, 0, result, 28, ciphertext.Length);
    return result;
}
```

### ❌ Chaves Criptográficas Fracas (EVITAR)

```csharp
// 💥 1024 bits para RSA - quebrável com recursos moderados
using var rsa = RSA.Create(1024);

// 💥 Chave hardcoded no código
return new byte[] { 0x01, 0x02, 0x03, 0x04, ... }; // Qualquer um com acesso ao código tem a chave!
```

```csharp
// ✅ CORRETO - RSA 2048+ bits
using var rsa = RSA.Create(2048); // ✅ Mínimo 2048 (melhor: 4096)

// ✅ CORRETO - AES 256 bits gerado aleatoriamente
var key = new byte[32]; // 256 bits
using var rng = RandomNumberGenerator.Create();
rng.GetBytes(key);

// ✅ MELHOR - Chaves do Azure Key Vault
var keyVaultClient = new SecretClient(
    new Uri("https://myvault.vault.azure.net/"),
    new DefaultAzureCredential());
var secret = await keyVaultClient.GetSecretAsync(keyName);
```

### ❌ IV Estático ou Previsível (EVITAR)

```csharp
private static readonly byte[] StaticIV = new byte[16]; // 💥 IV fixo

aes.IV = StaticIV;              // 💥 Mensagens idênticas = ciphertext idêntico
aes.IV = BitConverter.GetBytes(counter).PadToLength(16); // 💥 IV previsível
aes.IV = BitConverter.GetBytes(DateTime.Now.Ticks).PadToLength(16); // 💥 Timestamp previsível
```

```csharp
// ✅ CORRETO - IV aleatório por mensagem
aes.GenerateIV(); // Sempre diferente
```

### Regras de Criptografia

| Algoritmo | Status | Substituição |
|-----------|--------|--------------|
| DES | ❌ PROIBIDO | AES-256 |
| 3DES/TripleDES | ❌ PROIBIDO | AES-256 |
| RC2 | ❌ PROIBIDO | AES-256 |
| MD5 (para crypto) | ❌ PROIBIDO | SHA-256+ |
| SHA-1 (para crypto) | ❌ PROIBIDO | SHA-256+ |
| RSA < 2048 bits | ❌ PROIBIDO | RSA 2048+ |
| ECB mode | ❌ PROIBIDO | CBC ou GCM |
| IV estático | ❌ PROIBIDO | `aes.GenerateIV()` |
| AES-256-CBC | ✅ APROVADO | — |
| AES-256-GCM | ✅ MELHOR | — |
| RSA 2048+ | ✅ APROVADO | — |
| ECDSA P-256+ | ✅ APROVADO | — |

---

## 🔑 2. Autenticação e Hashing de Senhas

### ❌ Senha de Banco Fraca (EVITAR)

```csharp
// 💥 CRÍTICO: Senha vazia ou hardcoded
private const string ConnectionString1 =
    "Server=myserver;Database=mydb;User Id=admin;Password=;"; // 💥 Vazia
private const string ConnectionString2 =
    "Server=myserver;Database=mydb;User Id=admin;Password=admin;"; // 💥 "admin"
private const string ConnectionString3 =
    "Server=myserver;Database=mydb;User Id=sa;Password=MyP@ssw0rd123;"; // 💥 Hardcoded
```

```csharp
// ✅ CORRETO - Senha via variável de ambiente ou Key Vault
public class SecureDatabase(IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not configured");
}

// ✅ MELHOR - Azure Managed Identity (SEM SENHA)
var connection = new SqlConnection(connectionString);
var credential = new DefaultAzureCredential();
var token = await credential.GetTokenAsync(
    new TokenRequestContext(["https://database.windows.net/.default"]));
connection.AccessToken = token.Token;
await connection.OpenAsync();
```

### ❌ Hash de Senha Sem Salt (EVITAR)

```csharp
// 💥 CRÍTICO: Hash sem salt - rainbow tables quebram em segundos
using var sha256 = SHA256.Create();
var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

// 💥 CRÍTICO: Salt estático - um rainbow table quebra TODAS as senhas
const string staticSalt = "MyAppSalt2024";
var combined = password + staticSalt;

// 💥 CRÍTICO: MD5 tem colisões conhecidas
using var md5 = MD5.Create();
var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
```

```csharp
// ✅ CORRETO - PBKDF2 com salt único por usuário
private const int SaltSize = 16;    // 128 bits
private const int HashSize = 32;    // 256 bits
private const int Iterations = 100000; // OWASP: 100k+ para SHA-256

public (byte[] Hash, byte[] Salt) HashPassword(string password)
{
    var salt = new byte[SaltSize];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(salt); // ✅ Salt único e aleatório

    using var pbkdf2 = new Rfc2898DeriveBytes(
        password, salt, Iterations, HashAlgorithmName.SHA256);

    return (pbkdf2.GetBytes(HashSize), salt);
}

public bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
{
    using var pbkdf2 = new Rfc2898DeriveBytes(
        password, storedSalt, Iterations, HashAlgorithmName.SHA256);
    var hash = pbkdf2.GetBytes(HashSize);
    return CryptographicOperations.FixedTimeEquals(hash, storedHash); // ✅ Timing-safe
}

// ✅ MELHOR - ASP.NET Core Identity
services.AddDefaultIdentity<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
// Gerencia PBKDF2 automaticamente com 10000+ iterations
```

---

## 🔒 3. SSL/TLS

### ❌ Protocolo SSL/TLS Fraco (EVITAR)

```csharp
// 💥 SSL 3.0 e TLS 1.0 têm vulnerabilidades conhecidas (POODLE, BEAST)
ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;     // ❌ PROIBIDO
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;      // ❌ TLS 1.0
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;    // ❌ TLS 1.1
```

```csharp
// ✅ CORRETO - TLS 1.2 ou 1.3
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

// ✅ MELHOR - HttpClient moderno usa TLS 1.2/1.3 por padrão
var handler = new HttpClientHandler
{
    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
};
var httpClient = new HttpClient(handler);
```

### ❌ Certificado SSL Não Verificado (EVITAR)

```csharp
// 💥 CRÍTICO: Qualquer certificado é aceito - Man-in-the-Middle!
var handler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = 
        (message, cert, chain, errors) => true // ❌ NUNCA em produção!
};
```

```csharp
// ✅ CORRETO - Validação padrão (sempre)
var httpClient = new HttpClient(); // ✅ Validação SSL habilitada por padrão

// ✅ CORRETO - Para certificados auto-assinados em dev
var handler = new HttpClientHandler();
if (environment.IsDevelopment()) // ✅ SOMENTE em dev
{
    handler.ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
}
```

---

## 🎫 4. JWT e Tokens

### ❌ JWT Inseguro (EVITAR)

```csharp
// 💥 CRÍTICO: Algoritmo "none" - JWT sem assinatura
var header = Base64Encode("""{"alg":"none","typ":"JWT"}"""); // ❌ Sem assinatura!

// 💥 CRÍTICO: Segredo fraco ou hardcoded
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secret")); // ❌ Curto!
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("MyKey123")); // ❌ Hardcoded!

// 💥 CRÍTICO: Sem validação de expiração
var validationParameters = new TokenValidationParameters
{
    ValidateLifetime = false, // ❌ Tokens expirados aceitos!
    ValidateIssuer = false,   // ❌ Qualquer issuer aceito!
    ValidateAudience = false  // ❌ Qualquer audience aceito!
};
```

```csharp
// ✅ CORRETO - JWT com configuração segura
var validationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,          // ✅ Valida assinatura
    IssuerSigningKey = GetKeyFromVault(),     // ✅ Chave do Key Vault
    ValidateIssuer = true,                   // ✅ Valida issuer
    ValidIssuer = "https://my-auth-server",
    ValidateAudience = true,                 // ✅ Valida audience
    ValidAudience = "my-api",
    ValidateLifetime = true,                 // ✅ Valida expiração
    ClockSkew = TimeSpan.FromMinutes(5)
};

// ✅ Chave de no mínimo 256 bits para HMAC-SHA256
var key = new SymmetricSecurityKey(
    Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!
        .Select(c => (byte)c).ToArray()); // Ou melhor: do Key Vault
```

---

## 📦 5. Serialização e Deserialização

### ❌ Deserialização Insegura (EVITAR)

```csharp
// 💥 CRÍTICO: BinaryFormatter permite RCE (Remote Code Execution)
var formatter = new BinaryFormatter(); // ❌ BANIDO no .NET 5+
var obj = formatter.Deserialize(stream); // 💥 Pode executar código arbitrário!

// 💥 CRÍTICO: TypeNameHandling em Newtonsoft.Json
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.All // ❌ Permite injeção de tipo!
};
var obj = JsonConvert.DeserializeObject(json, settings);
```

```csharp
// ✅ CORRETO - System.Text.Json (seguro por padrão)
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
};
var obj = JsonSerializer.Deserialize<MyType>(json, options);

// ✅ CORRETO - Newtonsoft com TypeNameHandling seguro
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.None // ✅ Desabilita type injection
};
```

### ❌ XML External Entity (XXE) (EVITAR)

```csharp
// 💥 CRÍTICO: DTD processing habilitado permite XXE
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Parse // ❌ Processa DTD externas!
};

// 💥 Atacante pode ler /etc/passwd via:
// <?xml version="1.0"?>
// <!DOCTYPE foo [<!ENTITY xxe SYSTEM "file:///etc/passwd">]>
// <foo>&xxe;</foo>
```

```csharp
// ✅ CORRETO - DTD desabilitado
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit, // ✅ Proíbe DTD
    XmlResolver = null                       // ✅ Sem resolver externo
};
var reader = XmlReader.Create(stream, settings);
```

---

## 🛡️ Checklist de Segurança

### Criptografia
- [ ] AES-256-CBC ou AES-256-GCM (nunca DES/3DES/RC2)
- [ ] IV aleatório por mensagem (`aes.GenerateIV()`)
- [ ] Modo CBC ou GCM (nunca ECB)
- [ ] Chaves RSA ≥ 2048 bits
- [ ] Chaves em Azure Key Vault (não hardcoded)

### Autenticação e Senhas
- [ ] PBKDF2 com salt único e ≥ 100.000 iterações para senhas
- [ ] Senhas de banco via Key Vault ou Managed Identity (não hardcoded)
- [ ] JWT com ValidateLifetime, ValidateIssuer, ValidateAudience = true
- [ ] Segredo JWT ≥ 256 bits do Key Vault

### SSL/TLS
- [ ] TLS 1.2 ou 1.3 (nunca SSL 3.0, TLS 1.0, TLS 1.1)
- [ ] Certificados sempre validados (sem BypassSSL em produção)

### Serialização
- [ ] `System.Text.Json` (seguro por padrão)
- [ ] `TypeNameHandling.None` no Newtonsoft.Json
- [ ] `DtdProcessing.Prohibit` para XML
- [ ] `BinaryFormatter` banido completamente

### Guardrails do Projeto (copilot-instructions.md)
- [ ] SQL Injection: **SEMPRE** parâmetros nomeados no Dapper (`WHERE Id = @Id`)
- [ ] HTTP Externo: **NUNCA** `HttpClient` direto — usar `IProxyClient`
- [ ] Tipagem: **NUNCA** `dynamic` em retorno do Dapper

---

## 🚨 Consequências das Vulnerabilidades

| Vulnerabilidade | Consequência |
|-----------------|-------------|
| Weak cipher (DES/3DES) | Descriptografia de dados sensíveis – LGPD violação |
| Hash sem salt | Quebra de senhas via rainbow tables |
| JWT inseguro | Impersonation de usuários |
| DB senha hardcoded | Acesso irrestrito ao banco em qualquer ambiente |
| XXE | Leitura de arquivos do servidor, SSRF |
| BinaryFormatter | Remote Code Execution (RCE) completo |
| SSL cert bypass | Man-in-the-Middle – interceptação de dados |
| ECB mode | Padrões detectáveis em dados criptografados |

<!-- BEGIN:nextjs-agent-rules -->
# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` before writing any code. Heed deprecation notices.
<!-- END:nextjs-agent-rules -->

---

## Integração Blazor WASM ↔ Next.js

### Arquitetura

O Next.js App Shell carrega micro-frontends Blazor WASM via **Custom Elements** (Web Components). O runtime Blazor é injetado uma única vez no layout do dashboard e expõe componentes como tags HTML nativas.

```
Layout do Dashboard
└── BlazorHost          ← carrega _framework/blazor.webassembly.js (lazy)
    ├── /inventory      → <BlazorElement tag="inventory-grid" />
    └── /analytics      → <BlazorElement tag="analytics-dashboard" />
```

### Arquivos-chave

| Arquivo | Responsabilidade |
|---|---|
| `components/blazor-host.tsx` | Injeta o runtime Blazor WASM e MudBlazor via `<Script strategy="lazyOnload">`. Expõe `window.imsAuth.getToken()` para o Blazor via cookie público. |
| `components/blazor-element.tsx` | Wrapper React para Custom Elements Blazor. Faz polling até o Blazor estar pronto, mostra skeleton enquanto carrega. |
| `app/(dashboard)/layout.tsx` | Monta `<BlazorHost />` uma única vez para todo o dashboard. |
| `app/api/auth/me/route.ts` | Endpoint BFF que retorna `{ token: string }` com o token público. Usado pelo `BlazorHost` para inicializar `window.imsAuth`. |
| `public/_blazor/` | Artefatos do `dotnet publish` do Blazor. **Não commitar** — gerado pelo script de build. |

### Como usar um novo Custom Element Blazor

1. **Registrar no Blazor** (`frontend/apps/blazor-modules/Program.cs`):
   ```csharp
   builder.RootComponents.RegisterCustomElement<MeuComponente>("meu-componente");
   ```

2. **Declarar o tipo no TypeScript** (`components/blazor-element.tsx`):
   ```tsx
   // Adicionar em IntrinsicElements:
   'meu-componente': React.DetailedHTMLProps<...>;
   ```

3. **Adicionar ao tipo `tag`** em `BlazorElementProps`:
   ```tsx
   tag: 'inventory-grid' | 'analytics-dashboard' | 'meu-componente';
   ```

4. **Usar na página**:
   ```tsx
   <BlazorElement tag="meu-componente" apiBaseUrl="/api/proxy" />
   ```

### Build local (desenvolvimento)

```bash
# 1. Publicar Blazor e copiar para public/_blazor/
./scripts/build-blazor.sh

# 2. Iniciar Next.js
cd frontend/apps/next-shell
npm run dev
```

### Token de autenticação no Blazor

O Blazor acessa a API via BFF proxy em `/api/proxy/*`. O token é exposto via JS Interop:

```csharp
// No componente Blazor:
var token = await JSRuntime.InvokeAsync<string?>("window.imsAuth.getToken");
```

O cookie `ims_public_token` é **não HttpOnly** e contém apenas o token de curta duração para uso no cliente. O token HttpOnly de sessão permanece seguro no servidor.

### CI/CD

O workflow `ci.yml` possui 3 jobs em sequência:
1. `build-and-test` — .NET build + testes
2. `build-blazor` — `dotnet publish` Blazor, faz upload do `wwwroot` como artefato
3. `build-nextjs` — baixa artefato Blazor para `public/_blazor/`, executa `npm run build`

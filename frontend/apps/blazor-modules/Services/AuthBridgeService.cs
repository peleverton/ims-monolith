using Microsoft.JSInterop;

namespace BlazorModules.Services;

/// <summary>
/// Lê o JWT token exposto pelo Next.js via window.imsAuth.getToken()
/// Permite que o Blazor WASM autentique chamadas ao BFF sem gerenciar sessão própria.
/// </summary>
public interface IAuthBridgeService
{
    Task<string?> GetTokenAsync();
}

public class AuthBridgeService(IJSRuntime js) : IAuthBridgeService
{
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await js.InvokeAsync<string?>("imsAuth.getToken");
        }
        catch
        {
            // JS interop pode falhar em pré-renderização ou ambientes sem window
            return null;
        }
    }
}

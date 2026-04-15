using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using BlazorModules.Services;
using BlazorModules.Components;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// ── MudBlazor ────────────────────────────────────────────────
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.TopRight;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
});

// ── HttpClient ───────────────────────────────────────────────
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ── Serviços de negócio ───────────────────────────────────────
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuthBridgeService, AuthBridgeService>();

// ── Custom Elements — registra componentes como Web Components ──
// Método disponível via Microsoft.AspNetCore.Components.Web no .NET 9 Blazor WASM
builder.RootComponents.RegisterCustomElement<InventoryGrid>("inventory-grid");
builder.RootComponents.RegisterCustomElement<AnalyticsDashboard>("analytics-dashboard");

// Head outlet para standalone
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();


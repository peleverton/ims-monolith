using BlazorModules.Models;
using BlazorModules.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace BlazorModules.Components;

public partial class AnalyticsDashboard : IDisposable
{
    [Inject] private IAnalyticsService AnalyticsService { get; set; } = default!;
    [Parameter] public string ApiBaseUrl { get; set; } = "/api/proxy";

    private AnalyticsSummaryDto? _summary;
    private bool _loading = true;
    private Timer? _refreshTimer;

    // ── Gráfico de linha ──────────────────────────────────────
    private List<ChartSeries> _lineChartSeries = [];
    private string[] _lineLabels = [];
    private ChartOptions _lineOptions = new() { YAxisTicks = 5 };

    // ── Donut ─────────────────────────────────────────────────
    private double[] _donutData = [];
    private string[] _donutLabels = [];

    // ── Barras ────────────────────────────────────────────────
    private List<ChartSeries> _barSeries = [];
    private string[] _barLabels = ["Low", "Medium", "High", "Critical"];
    private ChartOptions _barOptions = new() { YAxisTicks = 5 };

    protected override async Task OnInitializedAsync()
    {
        await LoadData();

        // Refresh automático a cada 30 segundos
        _refreshTimer = new Timer(async _ =>
        {
            await LoadData();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private async Task LoadData()
    {
        _loading = true;
        _summary = await AnalyticsService.GetSummaryAsync();
        _loading = false;

        if (_summary is not null)
            BuildChartData(_summary);

        StateHasChanged();
    }

    private void BuildChartData(AnalyticsSummaryDto summary)
    {
        // Linha: issues por dia
        var days = summary.IssuesByDay.TakeLast(30).ToList();
        _lineLabels = days.Select(d => d.Date[5..]).ToArray(); // MM-DD
        _lineChartSeries =
        [
            new ChartSeries { Name = "Issues", Data = days.Select(d => (double)d.Count).ToArray() }
        ];

        // Donut: por status
        _donutLabels = summary.IssuesByStatus.Keys.ToArray();
        _donutData = summary.IssuesByStatus.Values.Select(v => (double)v).ToArray();

        // Barras: por prioridade
        _barSeries =
        [
            new ChartSeries
            {
                Name = "Issues",
                Data = _barLabels.Select(l => summary.IssuesByPriority.TryGetValue(l, out var v) ? (double)v : 0).ToArray()
            }
        ];
    }

    public void Dispose() => _refreshTimer?.Dispose();
}

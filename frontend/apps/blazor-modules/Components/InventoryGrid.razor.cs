using BlazorModules.Components.Dialogs;
using BlazorModules.Models;
using BlazorModules.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace BlazorModules.Components;

public partial class InventoryGrid
{
    [Inject] private IInventoryService InventoryService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>URL base da API — recebida como atributo do Custom Element</summary>
    [Parameter] public string ApiBaseUrl { get; set; } = "/api/proxy";

    private MudDataGrid<InventoryItemDto>? _grid;
    private string _search = string.Empty;

    private async Task<GridData<InventoryItemDto>> LoadServerData(GridState<InventoryItemDto> state)
    {
        var result = await InventoryService.GetItemsAsync(
            page: state.Page + 1,
            pageSize: state.PageSize,
            search: string.IsNullOrWhiteSpace(_search) ? null : _search
        );

        if (result is null)
            return new GridData<InventoryItemDto> { Items = [], TotalItems = 0 };

        return new GridData<InventoryItemDto>
        {
            Items = result.Items,
            TotalItems = result.TotalCount
        };
    }

    private void OnSearchChanged() => _grid?.ReloadServerData();

    private async Task OpenCreateDialog()
    {
        var dialog = await DialogService.ShowAsync<CreateItemDialog>("Novo Item de Inventário");
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await _grid!.ReloadServerData();
    }

    private async Task OpenEditDialog(InventoryItemDto item)
    {
        var parameters = new DialogParameters<EditItemDialog> { { x => x.Item, item } };
        var dialog = await DialogService.ShowAsync<EditItemDialog>("Editar Item", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await _grid!.ReloadServerData();
    }

    private async Task ConfirmDelete(InventoryItemDto item)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Confirmar exclusão",
            $"Tem certeza que deseja excluir '{item.Name}'?",
            yesText: "Excluir",
            cancelText: "Cancelar"
        );

        if (confirmed == true)
        {
            var success = await InventoryService.DeleteItemAsync(item.Id);
            if (success)
            {
                Snackbar.Add("Item excluído com sucesso.", Severity.Success);
                await _grid!.ReloadServerData();
            }
            else
            {
                Snackbar.Add("Erro ao excluir item.", Severity.Error);
            }
        }
    }

    private static Color GetStatusColor(ProductStatus status) => status switch
    {
        ProductStatus.Active => Color.Success,
        ProductStatus.Inactive => Color.Warning,
        ProductStatus.Discontinued => Color.Error,
        _ => Color.Default
    };
}

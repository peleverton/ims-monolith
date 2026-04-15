using BlazorModules.Models;
using BlazorModules.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace BlazorModules.Components.Dialogs;

public partial class EditItemDialog
{
    [Inject] private IInventoryService InventoryService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public InventoryItemDto Item { get; set; } = default!;

    private MudForm? _form;
    private bool _saving;

    private string _name = string.Empty;
    private int _quantity;
    private decimal _unitPrice;
    private string? _location;
    private ProductStatus _status;

    protected override void OnParametersSet()
    {
        _name = Item.Name;
        _quantity = Item.Quantity;
        _unitPrice = Item.UnitPrice;
        _location = Item.Location;
        _status = Item.Status;
    }

    private async Task Submit()
    {
        await _form!.Validate();
        if (!_form.IsValid) return;

        _saving = true;
        var request = new UpdateInventoryItemRequest(_name, _quantity, _unitPrice, _location, _status);
        var result = await InventoryService.UpdateItemAsync(Item.Id, request);
        _saving = false;

        if (result is not null)
        {
            Snackbar.Add("Item atualizado com sucesso!", Severity.Success);
            MudDialog.Close(DialogResult.Ok(result));
        }
        else
        {
            Snackbar.Add("Erro ao atualizar item.", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

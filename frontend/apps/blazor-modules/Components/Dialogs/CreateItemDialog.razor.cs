using BlazorModules.Models;
using BlazorModules.Services;
using MudBlazor;
using Microsoft.AspNetCore.Components;

namespace BlazorModules.Components.Dialogs;

public partial class CreateItemDialog
{
    [Inject] private IInventoryService InventoryService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    private MudForm? _form;
    private bool _saving;

    private CreateInventoryItemRequest _request = new()
    {
        Name = string.Empty,
        Sku = string.Empty,
        Quantity = 0,
        UnitPrice = 0m,
        Location = null
    };

    private async Task Submit()
    {
        await _form!.Validate();
        if (!_form.IsValid) return;

        _saving = true;
        var result = await InventoryService.CreateItemAsync(_request);
        _saving = false;

        if (result is not null)
        {
            Snackbar.Add("Item criado com sucesso!", Severity.Success);
            MudDialog.Close(DialogResult.Ok(result));
        }
        else
        {
            Snackbar.Add("Erro ao criar item.", Severity.Error);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

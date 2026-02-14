using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ScreenTranslator.App.ViewModels;

namespace ScreenTranslator.App.Pages;

public partial class SettingsPage : Page
{
    private bool _suppressModelPopup;

    public SettingsPage()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsViewModel>();
    }

    private void ModelSearchBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_suppressModelPopup) return;
        if (DataContext is SettingsViewModel vm && vm.HasModels)
        {
            vm.FilterModels(vm.OpenAiModel);
            ModelPopup.IsOpen = vm.FilteredModels.Count > 0;
        }
    }

    private void ModelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressModelPopup) return;
        if (DataContext is SettingsViewModel vm && vm.HasModels)
        {
            vm.FilterModels(vm.OpenAiModel);
            ModelPopup.IsOpen = vm.FilteredModels.Count > 0;
        }
    }

    private void ModelList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: ModelInfo model } && DataContext is SettingsViewModel vm)
        {
            _suppressModelPopup = true;
            vm.OpenAiModel = model.Id;
            vm.SelectedModelSupportsVision = model.SupportsVision;
            if (!model.SupportsVision)
                vm.UseVision = false;
            ModelPopup.IsOpen = false;
            _suppressModelPopup = false;
        }
    }
}

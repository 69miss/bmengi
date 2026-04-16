using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DoboEngineer.ViewModels;
using static System.Net.Mime.MediaTypeNames;

namespace DoboEngineer;

public partial class SpcUtility : Window
{
    SpcUtilityVm viewModel;
    public SpcUtility()
    {
        viewModel = new SpcUtilityVm();
        DataContext = viewModel;
        InitializeComponent();
        tbTxt.TextChanged += TbTxt_TextChanged;
        this.Closed += SpcUtility_Closed;
    }

    private void SpcUtility_Closed(object? sender, System.EventArgs e)
    {
        viewModel?.Dispose();
    }

    private void TbTxt_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!tbTxt.IsLoaded) return;

        var scrollViewer = tbTxt.FindControl<ScrollViewer>("PART_ScrollViewer");
        scrollViewer?.ScrollToEnd();
    }
}
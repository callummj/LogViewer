using System.Windows.Controls;
using LogViewerApp.ViewModels;

namespace LogViewerApp.Views;

public partial class LogTabView : UserControl
{
    public LogTabView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is LogTabViewModel vm)
                vm.ScrollToEntry = _ => LogGrid.ScrollIntoView(vm.SelectedEntry);
        };
    }
}

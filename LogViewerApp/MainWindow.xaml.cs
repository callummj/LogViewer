using System.Windows;
using LogViewerApp.ViewModels;

namespace LogViewerApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded  += async (_, _) => { if (Vm != null) await Vm.RestoreSessionAsync(); };
        Closing += (_, _) => Vm?.SaveSession();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;
}

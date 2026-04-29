using System.Windows;
using LogViewerApp.ViewModels;
using LogViewerApp.Views;

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

    private void ShowAbout_Click(object sender, RoutedEventArgs e)
        => new AboutWindow { Owner = this }.ShowDialog();
}

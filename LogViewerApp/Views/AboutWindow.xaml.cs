using System.Reflection;
using System.Windows;

namespace LogViewerApp.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "Unknown";

        var copyright = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyCopyrightAttribute>()
            ?.Copyright ?? string.Empty;

        VersionText.Text   = $"Version {version}";
        CopyrightText.Text = copyright;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

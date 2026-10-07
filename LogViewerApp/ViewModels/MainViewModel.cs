using Serilog;

namespace LogViewerApp.ViewModels;

public class MainViewModel : ViewModelBase, IMainViewModel
{
    private const string Identifier = "MainViewModel::";
    private readonly ILogger m_log;
    
    public MainViewModel(ILogger log)
    {
        m_log = log;

        Zoom = 1.0;
    }


    public double Zoom
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }
}
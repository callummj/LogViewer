namespace LogViewer.Lib.Config;

public class Config
{
    public string LogFileLocation { get; } =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\LogViewer\";
}
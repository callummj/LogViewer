namespace LogViewer.Lib.Config;

public class ConfigReader : IConfigReader
{
    public Task<Config> ReadConfigAsync(string fileLocation, CancellationToken cancel)
    {
        return Task.FromResult(new Config());
    }
}
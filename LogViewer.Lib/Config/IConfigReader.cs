namespace LogViewer.Lib.Config;

public interface IConfigReader
{
    public Task<Config> ReadConfigAsync(string fileLocation, CancellationToken cancel);
}
namespace LogViewerApp.Services;

public interface IDiContainer
{
    public T GetService<T>();
}
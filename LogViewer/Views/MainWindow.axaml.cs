using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace LogViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Only zoom if the Ctrl key is held down
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }
     
        // Determine how much to zoom per notch (e.g., 10%)
        double zoomStep = 0.1;
            
        // e.Delta.Y is positive when scrolling up, negative when scrolling down
        double currentZoom = ZoomBar.Value;
            
        if (e.Delta.Y > 0)
        {
            currentZoom += zoomStep; // Zoom in
        }
        else if (e.Delta.Y < 0)
        {
            currentZoom -= zoomStep; // Zoom out
        }

        // Constrain the calculated value within the ScrollBar's min/max bounds
        ZoomBar.Value = Math.Clamp(currentZoom, ZoomBar.Minimum, ZoomBar.Maximum);

        // Mark the event as handled so the ScrollViewer doesn't scroll vertically at the same time
        e.Handled = true;
    }
}
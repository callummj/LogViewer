using System.Windows;
using System.Windows.Input;

namespace LogViewerApp.Behaviours;

public class MouseWheelZoomBehavior
{
    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.RegisterAttached(
            "Zoom",
            typeof(double),
            typeof(MouseWheelZoomBehavior),
            new FrameworkPropertyMetadata(
                1.0,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnZoomChanged));

    public static void SetZoom(DependencyObject element, double value)
    {
        element.SetValue(ZoomProperty, value);
    }

    public static double GetZoom(DependencyObject element)
    {
        return (double)element.GetValue(ZoomProperty);
    }

    private static void OnZoomChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
            return;

        element.MouseWheel -= Element_MouseWheel;
        element.MouseWheel += Element_MouseWheel;
    }

    private static void Element_MouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
            return;

        if (sender is not FrameworkElement element)
            return;

        double zoom = GetZoom(element);

        zoom += e.Delta > 0 ? 0.1 : -0.1;

        zoom = Math.Clamp(zoom, 0.25, 5.0);

        SetZoom(element, zoom);

        e.Handled = true;
    }
}
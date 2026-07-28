using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LogViewerApp.ViewModels;

namespace LogViewerApp.Views;

public partial class LogTabView : UserControl
{
    private LogTabViewModel? _vm;

    public LogTabView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as LogTabViewModel;

        if (_vm != null)
        {
            _vm.ScrollToEntry = _ => LogGrid.ScrollIntoView(_vm.SelectedEntry);
            _vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogTabViewModel.IsFindOpen) && _vm!.IsFindOpen)
            Dispatcher.BeginInvoke(() => { FindBox.Focus(); FindBox.SelectAll(); });
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (_vm == null) return;
        switch (e.Key)
        {
            case Key.Return:
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    _vm.FindPreviousCommand.Execute(null);
                else
                    _vm.FindNextCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Escape:
                _vm.CloseFindCommand.Execute(null);
                LogGrid.Focus();
                e.Handled = true;
                break;
        }
    }
}

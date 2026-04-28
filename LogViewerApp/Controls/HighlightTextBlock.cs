using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace LogViewerApp.Controls;

public class HighlightTextBlock : TextBlock
{
    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(nameof(Highlight), typeof(string), typeof(HighlightTextBlock),
            new PropertyMetadata("", OnChanged));

    public static readonly DependencyProperty SourceTextProperty =
        DependencyProperty.Register(nameof(SourceText), typeof(string), typeof(HighlightTextBlock),
            new PropertyMetadata("", OnChanged));

    public string Highlight
    {
        get => (string)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    public string SourceText
    {
        get => (string)GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((HighlightTextBlock)d).Rebuild();

    private void Rebuild()
    {
        Inlines.Clear();
        var text    = SourceText ?? "";
        var keyword = Highlight ?? "";

        if (string.IsNullOrEmpty(keyword))
        {
            Inlines.Add(text);
            return;
        }

        int pos = 0;
        while (true)
        {
            int idx = text.IndexOf(keyword, pos, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                Inlines.Add(text[pos..]);
                break;
            }
            if (idx > pos) Inlines.Add(text[pos..idx]);
            Inlines.Add(new Run(text[idx..(idx + keyword.Length)])
            {
                Background = Brushes.Yellow,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            });
            pos = idx + keyword.Length;
        }
    }
}

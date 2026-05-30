using Microsoft.Xaml.Behaviors;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimeLogger.Behaviors;

public class ClickToOpenUrlBehavior : Behavior<TextBlock>
{
    protected override void OnAttached()
    {
        AssociatedObject.MouseLeftButtonUp += OnClick;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.MouseLeftButtonUp -= OnClick;
    }

    void OnClick(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.Text is string url && !string.IsNullOrWhiteSpace(url))
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WARNING] Failed to open URL: {ex.Message}");
            }
        }
    }
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimeLogger.Controls;

public class SimplePanel : Panel
{
    protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
    {
        var maxSize = new System.Windows.Size();
        foreach (System.Windows.UIElement child in InternalChildren)
        {
            if (child != null)
            {
                child.Measure(availableSize);
                maxSize.Width = Math.Max(maxSize.Width, child.DesiredSize.Width);
                maxSize.Height = Math.Max(maxSize.Height, child.DesiredSize.Height);
            }
        }
        return maxSize;
    }

    protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize)
    {
        foreach (System.Windows.UIElement child in InternalChildren)
        {
            child?.Arrange(new System.Windows.Rect(arrangeSize));
        }
        return arrangeSize;
    }
}

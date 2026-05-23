using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TimeLogger.Services;
using TimeLogger.ViewModels;

namespace TimeLogger;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new DialogService(this));
    }

    #region [Alternative code-behind technique for swipe behavior]
    /**
     **  This is not as smooth as the XAML Storyboard-based approach, but it works and is more straightforward to understand.
     **  I have given in to the OnAttached() OnDetaching() temptation supplied by the Microsoft.Xaml.Behaviors.Wpf NuGet package.
     **/
    System.Windows.Point _swipeStart;
    bool _swipeActive;
    const double DeadZone = 6;
    const double ActionThreshold = 120;

    Border? GetSwipeContent(ListBoxItem item)
    {
        return Extensions.FindChild<Border>(item, "SwipeContent");
        //return (Border)item.Template.FindName("SwipeContent", item);
    }

    void SwipeItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = (ListBoxItem)sender;
        _swipeStart = e.GetPosition(item);
        _swipeActive = true;
        item.CaptureMouse();

        var content = GetSwipeContent(item);
        if (content == null) 
            return;
        if (content.RenderTransform is not TranslateTransform)
            content.RenderTransform = new TranslateTransform();
    }

    void SwipeItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_swipeActive) 
            return;

        var item = (ListBoxItem)sender;
        var pos = e.GetPosition(item);

        double dx = pos.X - _swipeStart.X;
        double dy = pos.Y - _swipeStart.Y;

        // Only activate swipe if horizontal movement dominates
        if (Math.Abs(dx) < DeadZone || Math.Abs(dx) < Math.Abs(dy))
            return;

        // Now we know it's a horizontal swipe → block selection + clicks
        e.Handled = true;

        var content = GetSwipeContent(item);
        if (content?.RenderTransform is TranslateTransform tt)
            tt.X = dx;
    }

    void SwipeItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_swipeActive) 
            return;
        _swipeActive = false;

        var item = (ListBoxItem)sender;
        item.ReleaseMouseCapture();

        var content = GetSwipeContent(item);
        if (content == null)
            return;
        if (content.RenderTransform is not TranslateTransform tt)
            return;

        double dx = tt.X;

        if (dx <= -ActionThreshold)
            ExecuteItemCommand(item, "DeleteEntryCommand");
        else if (dx >= ActionThreshold)
            ExecuteItemCommand(item, "EditEntryCommand");

        tt.X = 0;
    }

    void ExecuteItemCommand(ListBoxItem item, string commandName)
    {
        if (DataContext is not MainViewModel vm) 
            return;

        var cmdProp = vm.GetType().GetProperty(commandName);
        if (cmdProp?.GetValue(vm) is ICommand cmd)
        {
            var entry = item.DataContext;
            if (cmd.CanExecute(entry))
                cmd.Execute(entry);
        }
    }

  
    #endregion
}
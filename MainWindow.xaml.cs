using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using TimeLogger.Services;
using TimeLogger.ViewModels;

namespace TimeLogger;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //SourceInitialized += (s, e) => DarkTitleBar.Apply(this); // if not using XAML approach
        //this.PreviewMouseRightButtonDown += (s, e) =>
        //{
        //    if (DataContext is not MainViewModel vm)
        //        return;
        //    vm.ShowChart(chart, listing);
        //};
        DataContext = new MainViewModel(new DialogService(this));
        #region [Example of fetching window from separate module]
        // Get the active window on the Dispatcher thread:
        //var window1 = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        // Get the window that owns the Dispatcher (UI thread):
        //var window2 = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.Dispatcher == App.Current.MainWindow.Dispatcher);

        // Get the window from a UI element (when available):
        //var window3 = Window.GetWindow(root);
        #endregion
    }

    public void FireMouseEvent()
    {
        Mouse.Capture(null);
        var args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        };
        RaiseEvent(args);
    }

    #region [Alternative code-behind technique for swipe behavior]
    /**
     **  This is not as smooth as the XAML Storyboard-based approach, but it works and is more straightforward to understand.
     **  I have given in to the OnAttached() OnDetaching() temptation supplied by the Microsoft.Xaml.Behaviors.Wpf NuGet package.
     **/
    System.Windows.Point _swipeStart;
    bool _swipeActive;
    const double DeadZone = 6;
    const double ActionThreshold = 90;

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

    /// <summary>
    /// Calls a VM RelayCommand by name, passing the ListBoxItem's DataContext as the command parameter.
    /// This is a bit hacky but it keeps the code-behind free of any specific VM types or references.
    /// The command name is passed as a string to avoid having to cast the DataContext to MainViewModel.
    /// </summary>
    /// <param name="item"><see cref="ListBoxItem"/></param>
    /// <param name="commandName">name of the <see cref="RelayCommand"/></param>
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
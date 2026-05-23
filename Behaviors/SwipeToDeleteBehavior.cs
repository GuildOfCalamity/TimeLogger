using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Xaml.Behaviors;

namespace TimeLogger.Behaviors;

/// <summary>
/// I've implemented this behavior in two ways: as a Microsoft.Xaml.Behaviors NuGet package behavior (SwipeBehavior) and as a home-brew attached property (SwipeToDeleteOrEditBehavior).
/// </summary>
public class SwipeBehavior : Behavior<System.Windows.FrameworkElement>
{
    #region [Properties]
    System.Windows.Point _start;
    bool _swiping;
    const double DeadZone = 6;
    const double ActionThreshold = 90; // how far (in pixels) to drag before triggering action
    double _lastX = 0;
    bool _hasMoved = false;

    public ICommand EditCommand
    {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(SwipeBehavior));

    public ICommand DeleteCommand
    {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(SwipeBehavior));
    #endregion

    protected override void OnAttached()
    {
        AssociatedObject.PreviewMouseLeftButtonDown += OnDown;
        AssociatedObject.PreviewMouseMove += OnMove;
        AssociatedObject.PreviewMouseLeftButtonUp += OnUp;

        if (AssociatedObject.RenderTransform is not TranslateTransform)
            AssociatedObject.RenderTransform = new TranslateTransform();
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnDown;
        AssociatedObject.PreviewMouseMove -= OnMove;
        AssociatedObject.PreviewMouseLeftButtonUp -= OnUp;
    }

    void OnDown(object sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(AssociatedObject);
        _swiping = true;
        _hasMoved = false;
        _lastX = 0;

        AssociatedObject.CaptureMouse();
    }

    void OnMove(object sender, MouseEventArgs e)
    {
        if (!_swiping) return;

        var pos = e.GetPosition(AssociatedObject);
        double dx = pos.X - _start.X;
        double dy = pos.Y - _start.Y;

        // Only horizontal movement triggers swipe
        if (Math.Abs(dx) < DeadZone || Math.Abs(dx) < Math.Abs(dy))
            return;

        e.Handled = true;
        _hasMoved = true;

        // Smooth movement: weighted interpolation
        double smoothed = (_lastX * 0.7) + (dx * 0.3);

        if (AssociatedObject.RenderTransform is TranslateTransform tt)
            tt.X = smoothed;

        _lastX = smoothed;
    }

    void OnUp(object sender, MouseButtonEventArgs e)
    {
        if (!_swiping) return;
        _swiping = false;

        AssociatedObject.ReleaseMouseCapture();

        if (AssociatedObject.RenderTransform is not TranslateTransform tt)
            return;

        double dx = tt.X;

        if (_hasMoved)
        {
            if (dx >= ActionThreshold)
            {
                if (EditCommand?.CanExecute(AssociatedObject.DataContext) == true)
                    EditCommand.Execute(AssociatedObject.DataContext);
            }
            else if (dx <= -ActionThreshold)
            {
                if (DeleteCommand?.CanExecute(AssociatedObject.DataContext) == true)
                    DeleteCommand.Execute(AssociatedObject.DataContext);
            }
        }

        // Snap back (no animation = no frozen transform)
        tt.X = 0;
    }
}

/// <summary>
/// This home-brew version does not work as well as the Microsoft.Xaml.Behaviors NuGet package.
/// </summary>
public static class SwipeToDeleteOrEditBehavior
{
    #region [Attached Properties]
    public static readonly DependencyProperty DeleteCommandProperty =
        DependencyProperty.RegisterAttached(
            "DeleteCommand",
            typeof(ICommand),
            typeof(SwipeToDeleteOrEditBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EditCommandProperty =
        DependencyProperty.RegisterAttached(
            "EditCommand",
            typeof(ICommand),
            typeof(SwipeToDeleteOrEditBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty EnableSwipeProperty =
        DependencyProperty.RegisterAttached(
            "EnableSwipe",
            typeof(bool),
            typeof(SwipeToDeleteOrEditBehavior),
            new PropertyMetadata(false, OnEnableSwipeChanged));
    #endregion

    public static void SetDeleteCommand(DependencyObject obj, ICommand value) => obj.SetValue(DeleteCommandProperty, value);
    public static ICommand GetDeleteCommand(DependencyObject obj) => (ICommand)obj.GetValue(DeleteCommandProperty);
    public static void SetEditCommand(DependencyObject obj, ICommand value) => obj.SetValue(EditCommandProperty, value);
    public static ICommand GetEditCommand(DependencyObject obj) => (ICommand)obj.GetValue(EditCommandProperty);
    public static void SetEnableSwipe(DependencyObject obj, bool value) => obj.SetValue(EnableSwipeProperty, value);
    public static bool GetEnableSwipe(DependencyObject obj) => (bool)obj.GetValue(EnableSwipeProperty);
    private static void OnEnableSwipeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        if ((bool)e.NewValue == false)
            return;

        element.Loaded += (_, __) =>
        {
            var transform = new TranslateTransform();
            element.RenderTransform = transform;

            System.Windows.Point start = default;
            bool dragging = false;
            bool moved = false;
            double lastX = 0;
            DateTime lastMoveTime = DateTime.Now;

            const double deadZone = 6;
            const double actionThreshold = 120; // how far (in pixels) to drag before triggering action

            element.MouseLeftButtonDown += (_, args) =>
            {
                start = args.GetPosition(element);
                dragging = true;
                moved = false;
                lastX = 0;
                element.CaptureMouse();
            };

            element.MouseMove += (_, args) =>
            {
                if (!dragging) return;

                var pos = args.GetPosition(element);
                double dx = pos.X - start.X;

                if (!moved && Math.Abs(dx) < deadZone)
                    return;

                moved = true;

                double smoothed = (lastX * 0.7) + (dx * 0.3);
                transform.X = smoothed;

                lastX = smoothed;
                lastMoveTime = DateTime.Now;
            };

            element.MouseLeftButtonUp += (_, args) =>
            {
                if (!dragging)
                    return;

                dragging = false;
                element.ReleaseMouseCapture();

                double finalX = transform.X;

                if (!moved)
                {
                    SnapBack(transform);
                    return;
                }

                double velocity = finalX / (DateTime.Now - lastMoveTime).TotalMilliseconds;
                bool isDelete = finalX < -actionThreshold || velocity < -0.8;
                bool isEdit = finalX > actionThreshold || velocity > 0.8;

                if (isDelete)
                {
                    SlideOutAndExecute(element, transform, -500, GetDeleteCommand(element));
                }
                else if (isEdit)
                {
                    SlideOutAndExecute(element, transform, 500, GetEditCommand(element), resetAfter: true);
                }
                else
                {
                    SnapBack(transform);
                }
            };
        };
    }

    static void SnapBack(TranslateTransform transform)
    {
        var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase()
        };
        transform.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    static void SlideOutAndExecute(FrameworkElement element, TranslateTransform transform, double target, ICommand command, bool resetAfter = false)
    {
        var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase()
        };

        anim.Completed += (_, __) =>
        {
            // Execute the command (edit or delete)
            if (command?.CanExecute(element.DataContext) == true)
                command.Execute(element.DataContext);

            // Reset position AFTER command executes (important for edit)
            if (resetAfter)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, null);
                transform.X = 0;
            }
        };

        transform.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    static void SlideOutAndExecute(FrameworkElement element, TranslateTransform transform, double target, ICommand command)
    {
        var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new QuadraticEase()
        };

        anim.Completed += (_, __) =>
        {
            if (command?.CanExecute(element.DataContext) == true)
                command.Execute(element.DataContext);
        };

        transform.BeginAnimation(TranslateTransform.XProperty, anim);
    }
}

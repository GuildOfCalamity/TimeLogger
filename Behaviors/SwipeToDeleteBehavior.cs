using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TimeLogger.Behaviors;

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
            const double actionThreshold = 120;

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

                bool isDelete =
                    finalX < -actionThreshold ||
                    velocity < -0.8;

                bool isEdit =
                    finalX > actionThreshold ||
                    velocity > 0.8;

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
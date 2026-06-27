using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using TimeLogger.Controls;

namespace TimeLogger.Behaviors;

public static class ListBoxBehaviors
{

    public static readonly DependencyProperty GlowColorProperty =
         DependencyProperty.RegisterAttached(
             "GlowColor",
             typeof(Color),
             typeof(ListBoxBehaviors),
             new PropertyMetadata(Colors.DeepSkyBlue));

    public static void SetGlowColor(DependencyObject obj, Color value)
        => obj.SetValue(GlowColorProperty, value);

    public static Color GetGlowColor(DependencyObject obj)
        => (Color)obj.GetValue(GlowColorProperty);

    public static readonly DependencyProperty ScrollToSelectedItemProperty =
        DependencyProperty.RegisterAttached(
            "ScrollToSelectedItem",
            typeof(bool),
            typeof(ListBoxBehaviors),
            new PropertyMetadata(false, OnChanged));

    public static void SetScrollToSelectedItem(DependencyObject obj, bool value) 
        => obj.SetValue(ScrollToSelectedItemProperty, value);

    public static bool GetScrollToSelectedItem(DependencyObject obj) 
        => (bool)obj.GetValue(ScrollToSelectedItemProperty);

    static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ListBox listBox && (bool)e.NewValue)
        {
            // We'll use a WeakEventManager here to avoid memory
            // leaks and strong references to the ListBox.
            WeakEventManager<ListBox, SelectionChangedEventArgs>
                .AddHandler(listBox, "SelectionChanged", OnSelectionChanged);
            /*
            listBox.SelectionChanged += (s, _) =>
            {
                if (listBox.SelectedItem != null)
                    listBox.Dispatcher.InvokeAsync(() =>
                        listBox.ScrollIntoView(listBox.SelectedItem));
            };
            */
        }
    }

    static void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is null || sender is not ListBox lb || lb.SelectedItem == null)
            return;

        // Scroll into view
        lb.ScrollIntoView(lb.SelectedItem);

        // Get the ListBoxItem
        var container = lb.ItemContainerGenerator.ContainerFromItem(lb.SelectedItem) as ListBoxItem;
        if (container == null)
            return;

        var glowColor = GetGlowColor(lb);

        //BorderFlash(container);
        Wiggle(container);
        GlowPulse(container, glowColor);
    }

    #region [Effects]
    static void Wiggle(ListBoxItem item)
    {
        // We must have a RenderTransform for this to work.
        var transform = item.RenderTransform as TranslateTransform;
        if (transform == null)
        {
            transform = new TranslateTransform();
            item.RenderTransform = transform;
        }

        var wiggle = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(350)
        };

        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(-4, KeyTime.FromPercent(0.2)));
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(4, KeyTime.FromPercent(0.4)));
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(-2, KeyTime.FromPercent(0.6)));
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0)));

        transform.BeginAnimation(TranslateTransform.XProperty, wiggle);
    }

    static void GlowPulse(ListBoxItem item, Color glowColor)
    {
        // Ensure the item has an effect
        var shadow = item.Effect as DropShadowEffect;
        if (shadow == null)
        {
            shadow = new DropShadowEffect
            {
                Color = glowColor,
                BlurRadius = 0,
                ShadowDepth = 0,
                Opacity = 0
            };
            item.Effect = shadow;
        }
        else
        {
            shadow.Color = glowColor;
        }

        // Animate blur radius (size of glow)
        var blurAnim = new DoubleAnimation
        {
            From = 0,
            To = 25,
            Duration = TimeSpan.FromMilliseconds(250),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        // Animate opacity (brightness of glow)
        var opacityAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(250),
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
    }

    static void BorderFlash(ListBoxItem item)
    {
        var borderBrush = new SolidColorBrush(Colors.Transparent);
        item.BorderBrush = borderBrush;
        item.BorderThickness = new Thickness(3);

        var flash = new ColorAnimation
        {
            From = Colors.DeepSkyBlue,
            To = Colors.Transparent,
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        borderBrush.BeginAnimation(SolidColorBrush.ColorProperty, flash);
    }

    static void BackgroundFlash(ListBoxItem item)
    {
        var highlight = new ColorAnimation
        {
            From = Color.FromArgb(200, 255, 255, 255),   // bright flash
            To = Color.FromArgb(200, 60, 60, 60),        // normal dark theme background
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };

        var brush = new SolidColorBrush(Color.FromArgb(200, 60, 60, 60));
        item.Background = brush;

        brush.BeginAnimation(SolidColorBrush.ColorProperty, highlight);
    }
    #endregion
}

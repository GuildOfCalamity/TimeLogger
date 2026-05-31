using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Xaml.Behaviors;
using Microsoft.Xaml.Behaviors.Core;

namespace TimeLogger.Behaviors;

/// <summary>
/// A behavior that creates a wobble effect on a button when the mouse hovers over it.
/// </summary>
public class WobbleMouseHoverBehavior : Behavior<Button>
{
    bool _isAnimating = false;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseEnter += OnMouseEnter;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseEnter -= OnMouseEnter;
    }

    void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        AssociatedObject.RenderTransformOrigin = new Point(0.5, 0.5);

        if (AssociatedObject.RenderTransform is not RotateTransform rotate)
        {
            rotate = new RotateTransform(0);
            AssociatedObject.RenderTransform = rotate;
        }

        // Create wobble animation
        var wobble = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(350)
        };
            
        // Goes from 0 to 3 degrees, then -2, then 1, and back to 0 for wobble effect
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(3, KeyTime.FromPercent(0.2), new CubicEase { EasingMode = EasingMode.EaseOut }));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(-2, KeyTime.FromPercent(0.45), new CubicEase { EasingMode = EasingMode.EaseOut }));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(0.7), new CubicEase { EasingMode = EasingMode.EaseOut }));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));

        // Reset angle when done
        wobble.Completed += (_, __) =>
        {
            rotate.Angle = 0;
            _isAnimating = false;
        };

        rotate.BeginAnimation(RotateTransform.AngleProperty, wobble);
    }
}

public enum GlowTriggerMode
{
    Hover,
    Click,
    HoverAndClick
}

/// <summary>
/// A behavior that creates a glowing effect around a button when the mouse hovers over it.
/// </summary>
public class GlowMouseHoverBehavior : Behavior<Button>
{
    bool _isAnimating = false;
    
    #region [Dependency Properties]
    /// <summary>
    /// Animation Duration (ms)
    /// </summary>
    public int AnimationDuration
    {
        get => (int)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }
    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.Register(nameof(AnimationDuration), typeof(int),
            typeof(GlowMouseHoverBehavior), new PropertyMetadata(500));

    /// <summary>
    /// Bloom Intensity (max blur radius)
    /// </summary>
    public double BloomIntensity
    {
        get => (double)GetValue(BloomIntensityProperty);
        set => SetValue(BloomIntensityProperty, value);
    }
    public static readonly DependencyProperty BloomIntensityProperty =
        DependencyProperty.Register(nameof(BloomIntensity), typeof(double),
            typeof(GlowMouseHoverBehavior), new PropertyMetadata(20.0));

    /// <summary>
    /// Bloom Color (the DropShadowEffect's color property)
    /// </summary>
    public Color BloomColor
    {
        get => (Color)GetValue(BloomColorProperty);
        set => SetValue(BloomColorProperty, value);
    }
    public static readonly DependencyProperty BloomColorProperty =
        DependencyProperty.Register(nameof(BloomColor), typeof(Color),
            typeof(GlowMouseHoverBehavior), new PropertyMetadata(Colors.DodgerBlue));

    /// <summary>
    /// Trigger Mode (Hover, Click, Both)
    /// </summary>
    public GlowTriggerMode TriggerMode
    {
        get => (GlowTriggerMode)GetValue(TriggerModeProperty);
        set => SetValue(TriggerModeProperty, value);
    }
    public static readonly DependencyProperty TriggerModeProperty =
        DependencyProperty.Register(nameof(TriggerMode), typeof(GlowTriggerMode),
            typeof(GlowMouseHoverBehavior), new PropertyMetadata(GlowTriggerMode.Hover));
    #endregion

    protected override void OnAttached()
    {
        base.OnAttached();
        #region [Select event based on trigger]
        if (TriggerMode is GlowTriggerMode.Hover or GlowTriggerMode.HoverAndClick)
            AssociatedObject.MouseEnter += OnTrigger;

        if (TriggerMode is GlowTriggerMode.Click or GlowTriggerMode.HoverAndClick)
            AssociatedObject.PreviewMouseLeftButtonDown += OnTrigger;
        #endregion
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseEnter -= OnTrigger;
        AssociatedObject.PreviewMouseLeftButtonDown -= OnTrigger;
    }

    void OnTrigger(object sender, RoutedEventArgs e)
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        // Ensure glow effect exists
        if (AssociatedObject.Effect is not DropShadowEffect shadow)
        {
            shadow = new DropShadowEffect
            {
                Color = Colors.DeepSkyBlue,
                BlurRadius = 0,
                ShadowDepth = 0,
                Opacity = 0
            };
            AssociatedObject.Effect = shadow;
        }

        var duration = TimeSpan.FromMilliseconds(AnimationDuration);
        double maxBlur = BloomIntensity;

        //
        // ⭐ Glow Opacity Animation
        //
        var glow = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration
        };

        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0.8, KeyTime.FromPercent(0.25), new CubicEase { EasingMode = EasingMode.EaseOut }));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0.4, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));


        //
        // ⭐ Blur Radius Animation
        //
        var blur = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration
        };

        blur.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(maxBlur, KeyTime.FromPercent(0.25), new CubicEase { EasingMode = EasingMode.EaseOut }));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(maxBlur * 0.45, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));


        glow.Completed += (_, __) =>
        {
            shadow.Opacity = 0;
            shadow.BlurRadius = 0;
            _isAnimating = false;
        };

        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, glow);
        shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blur);
    }

    #region [Previous Technique]
    void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        AssociatedObject.RenderTransformOrigin = new Point(0.5, 0.5);

        // Ensure glow effect exists
        if (AssociatedObject.Effect is not DropShadowEffect shadow)
        {
            shadow = new DropShadowEffect
            {
                Color = BloomColor,
                BlurRadius = 0,
                ShadowDepth = 0,
                Opacity = 0
            };
            AssociatedObject.Effect = shadow;
        }

        var duration = TimeSpan.FromMilliseconds(AnimationDuration);
        double maxBlur = BloomIntensity;

        //
        // Glow Opacity Animation
        //
        var glow = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration
        };

        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromPercent(0.25), new CubicEase { EasingMode = EasingMode.EaseOut }));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0.5, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));


        //
        // Blur Radius Animation (scaled by BloomIntensity)
        //
        var blur = new DoubleAnimationUsingKeyFrames
        {
            Duration = duration
        };

        blur.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(maxBlur, KeyTime.FromPercent(0.25), new CubicEase { EasingMode = EasingMode.EaseOut }));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(maxBlur * 0.45, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));

        // When glow completes reset state
        glow.Completed += (_, __) =>
        {
            shadow.Opacity = 0;
            shadow.BlurRadius = 0;
            _isAnimating = false;
        };

        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, glow);
        shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blur);
    }
    #endregion
}


/// <summary>
/// A behavior that combines both the wobble and glow effects when the mouse hovers over a button.
/// </summary>
public class WobbleGlowMouseHoverBehavior : Behavior<Button>
{
    bool _isAnimating = false;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseEnter += OnMouseEnter;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseEnter -= OnMouseEnter;
    }

    void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (_isAnimating)
            return;

        _isAnimating = true;

        AssociatedObject.RenderTransformOrigin = new Point(0.5, 0.5);

        // Ensure rotate transform exists
        if (AssociatedObject.RenderTransform is not RotateTransform rotate)
        {
            rotate = new RotateTransform(0);
            AssociatedObject.RenderTransform = rotate;
        }

        // Ensure glow effect exists
        if (AssociatedObject.Effect is not DropShadowEffect shadow)
        {
            shadow = new DropShadowEffect
            {
                Color = Colors.DeepSkyBlue,
                BlurRadius = 0,
                ShadowDepth = 0,
                Opacity = 0
            };
            AssociatedObject.Effect = shadow;
        }

        //
        // Wobble Animation
        //
        var wobble = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(350)
        };

        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(3, KeyTime.FromPercent(0.2), new CubicEase { EasingMode = EasingMode.EaseOut }));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(-2, KeyTime.FromPercent(0.45), new CubicEase { EasingMode = EasingMode.EaseOut }));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromPercent(0.7), new CubicEase { EasingMode = EasingMode.EaseOut }));
        wobble.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));

        //
        // Glow/Pulse Animation
        //
        var glow = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(350)
        };

        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0.8, KeyTime.FromPercent(0.25), new CubicEase { EasingMode = EasingMode.EaseOut }));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0.4, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
        glow.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));

        // Blur radius pulse (soft expansion)
        var blur = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(350)
        };

        blur.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0.0)));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(24, KeyTime.FromPercent(0.25), new CubicEase { EasingMode = EasingMode.EaseOut }));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromPercent(0.5), new CubicEase { EasingMode = EasingMode.EaseOut }));
        blur.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1.0), new CubicEase { EasingMode = EasingMode.EaseOut }));

        // When wobble completes, reset state
        wobble.Completed += (_, __) =>
        {
            rotate.Angle = 0;
            shadow.Opacity = 0;
            shadow.BlurRadius = 0;
            _isAnimating = false;
        };

        // Start animations
        rotate.BeginAnimation(RotateTransform.AngleProperty, wobble);
        shadow.BeginAnimation(DropShadowEffect.OpacityProperty, glow);
        shadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blur);
    }
}

/// <summary>
/// A behavior that changes the rotation angle of a button when the mouse 
/// hovers over it, creating a teeter-totter visual effect.
/// Could be used on an image to allow the user to rotate assets in real-time.
/// </summary>
public class ChangeAngleMouseHoverBehavior : Behavior<Button>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseEnter += AssociatedObject_MouseEnter;
        AssociatedObject.MouseLeave += AssociatedObject_MouseLeave;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseEnter -= AssociatedObject_MouseEnter;
        AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
    }

    void AssociatedObject_MouseEnter(object sender, MouseEventArgs e) => UpdateAngle(e);

    void AssociatedObject_MouseLeave(object sender, MouseEventArgs e) => UpdateAngle(e);

    void UpdateAngle(MouseEventArgs e)
    {
        if (AssociatedObject == null || AssociatedObject.ActualWidth.IsInvalidOrZero() || AssociatedObject.ActualHeight.IsInvalidOrZero())
            return;

        AssociatedObject.RenderTransformOrigin = new Point(0.5, 0.5);
        if (AssociatedObject.RenderTransform is not RotateTransform rotateTransform)
        {
            rotateTransform = new RotateTransform();
            AssociatedObject.RenderTransform = rotateTransform;
        }
        var point = e.GetPosition(AssociatedObject.Parent as UIElement);
        var centerPoint = new Point(AssociatedObject.ActualWidth / 2, AssociatedObject.ActualHeight / 2);
        var angleOfLine = Math.Atan2(point.Y - centerPoint.Y, point.X - centerPoint.X) * 180 / Math.PI;
        rotateTransform.Angle = angleOfLine + 180;
    }
}

#region [Helper Classes]
public class ExtendedVisualStateManager
{
    internal static Rect GetLayoutRect(FrameworkElement element)
    {
        var actualWidth = element.ActualWidth;
        var actualHeight = element.ActualHeight;
        if (element is Image || element is MediaElement)
            if (element.Parent is Canvas)
            {
                actualWidth = double.IsNaN(element.Width) ? actualWidth : element.Width;
                actualHeight = double.IsNaN(element.Height) ? actualHeight : element.Height;
            }
            else
            {
                actualWidth = element.RenderSize.Width;
                actualHeight = element.RenderSize.Height;
            }
        actualWidth = element.Visibility == Visibility.Collapsed ? 0.0 : actualWidth;
        actualHeight = element.Visibility == Visibility.Collapsed ? 0.0 : actualHeight;
        var margin = element.Margin;
        var layoutSlot = LayoutInformation.GetLayoutSlot(element);
        var x = 0.0;
        var y = 0.0;
        switch (element.HorizontalAlignment)
        {
            case HorizontalAlignment.Left:
                x = layoutSlot.Left + margin.Left;
                break;

            case HorizontalAlignment.Center:
                x = (layoutSlot.Left + margin.Left + layoutSlot.Right - margin.Right) / 2.0 - actualWidth / 2.0;
                break;

            case HorizontalAlignment.Right:
                x = layoutSlot.Right - margin.Right - actualWidth;
                break;

            case HorizontalAlignment.Stretch:
                x = Math.Max(layoutSlot.Left + margin.Left,
                    (layoutSlot.Left + margin.Left + layoutSlot.Right - margin.Right) / 2.0 - actualWidth / 2.0);
                break;
        }
        switch (element.VerticalAlignment)
        {
            case VerticalAlignment.Top:
                y = layoutSlot.Top + margin.Top;
                break;

            case VerticalAlignment.Center:
                y = (layoutSlot.Top + margin.Top + layoutSlot.Bottom - margin.Bottom) / 2.0 - actualHeight / 2.0;
                break;

            case VerticalAlignment.Bottom:
                y = layoutSlot.Bottom - margin.Bottom - actualHeight;
                break;

            case VerticalAlignment.Stretch:
                y = Math.Max(layoutSlot.Top + margin.Top,
                    (layoutSlot.Top + margin.Top + layoutSlot.Bottom - margin.Bottom) / 2.0 - actualHeight / 2.0);
                break;
        }
        return new Rect(x, y, actualWidth, actualHeight);
    }
}
#endregion

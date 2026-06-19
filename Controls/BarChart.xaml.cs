using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

using TimeLogger.Models;

namespace TimeLogger.Controls
{
    public partial class BarChart : UserControl
    {
        public BarChart()
        {
            InitializeComponent();
            SizeChanged += (_, _) => RedrawEntries();
            IsVisibleChanged += (_, _) => RedrawEntries();
        }

        public List<TaskEntry> Entries
        {
            get => (List<TaskEntry>)GetValue(EntriesProperty);
            set => SetValue(EntriesProperty, value);
        }
        public static readonly DependencyProperty EntriesProperty = DependencyProperty.Register(
            nameof(Entries),
            typeof(List<TaskEntry>),
            typeof(BarChart),
    new FrameworkPropertyMetadata(null, OnEntriesChanged));
        static void OnEntriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BarChart chart)
                chart.RedrawEntries();
        }

        public static readonly DependencyProperty BarFillColorProperty = DependencyProperty.Register(
            nameof(BarFillColor),
            typeof(Color),
            typeof(BarChart),
            new PropertyMetadata(Color.FromArgb(100, 100, 160, 255)));
        public Color BarFillColor
        {
            get => (Color)GetValue(BarFillColorProperty);
            set => SetValue(BarFillColorProperty, value);
        }

        public static readonly DependencyProperty BarBorderColorProperty = DependencyProperty.Register(
            nameof(BarBorderColor),
            typeof(Color),
            typeof(BarChart),
            new PropertyMetadata(Color.FromArgb(150, 70, 120, 210)));
        public Color BarBorderColor
        {
            get => (Color)GetValue(BarBorderColorProperty);
            set => SetValue(BarBorderColorProperty, value);
        }

        public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
            nameof(TextColor),
            typeof(Color),
            typeof(BarChart),
    new PropertyMetadata(Color.FromArgb(200, 220, 220, 220)));
        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public static readonly DependencyProperty AnimateBarsProperty = DependencyProperty.Register(
            nameof(AnimateBars),
            typeof(bool),
            typeof(BarChart),
            new PropertyMetadata(true));

        public bool AnimateBars
        {
            get => (bool)GetValue(AnimateBarsProperty);
            set => SetValue(AnimateBarsProperty, value);
        }

        void RedrawEntries()
        {
            PART_BarCanvas.Children.Clear();

            if (Entries == null)
                return;

            var points = Entries;
            if (points.Count == 0)
                return;

            double maxValue = points.Max(p => p.TimeSpent.TotalHours);
            if (maxValue <= 0)
                maxValue = 1;

            double width = PART_BarCanvas.ActualWidth;
            double height = PART_BarCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                // Wait for layout
                Loaded += (_, __) => RedrawEntries();
                return;
            }

            double barWidth = width / points.Count * 0.6;
            double spacing = width / points.Count * 0.4;

            double x = spacing / 2;

            foreach (var p in points)
            {
                double barHeight = (p.TimeSpent.TotalHours / maxValue) * height;

                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = AnimateBars ? 0 : barHeight,
                    RadiusX = 4,
                    RadiusY = 4,
                    Fill = new SolidColorBrush(BarFillColor),
                    Stroke = new SolidColorBrush(BarBorderColor),
                    StrokeThickness = 2,
                    SnapsToDevicePixels = true
                };


                // Tooltip
                var tooltip = new ToolTip
                {
                    //PART_TooltipText.Text = $"{closest.Time.ToString("ddd MMM dd, yyyy")}\n{closest.Title}\n{closest.Value:0.00} {closest.Uom}";
                    Content = $"{p.Date.ToString("ddd MMM dd, yyyy")}\n{p.Description}\n{p.TimeSpent.TotalHours:0.00} hours",
                    Background = new SolidColorBrush(Color.FromArgb(120, 30, 30, 30)),
                    Foreground = Brushes.White,
                    Padding = new Thickness(8),
                    Margin = new Thickness(5),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                    BorderThickness = new Thickness(1),
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse
                };
                // Drop shadow
                tooltip.Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.4
                };

                ToolTipService.SetInitialShowDelay(rect, 0);
                ToolTipService.SetShowDuration(rect, 5000);
                rect.ToolTip = tooltip;

                if (AnimateBars)
                {
                    // Start collapsed at bottom
                    rect.Height = 0;
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, height); // bottom of chart
                }
                else
                {
                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, height - barHeight);
                }

                PART_BarCanvas.Children.Add(rect);

                if (AnimateBars)
                {
                    var growAnimation = new DoubleAnimation
                    {
                        From = 0,
                        To = barHeight,
                        Duration = TimeSpan.FromMilliseconds(2000),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    // As height changes, keep the bar anchored to the bottom
                    growAnimation.CurrentTimeInvalidated += (s, _) =>
                    {
                        double currentHeight = rect.Height;
                        Canvas.SetTop(rect, height - currentHeight);
                    };

                    // Ensure final position is correct
                    growAnimation.Completed += (_, __) =>
                    {
                        Canvas.SetTop(rect, height - barHeight);
                    };

                    rect.BeginAnimation(FrameworkElement.HeightProperty, growAnimation);
                }

                // Inside bar text
                var valueText = new TextBlock
                {
                    Text = $"{p.TimeSpent.TotalHours:0.0}",
                    Foreground = new SolidColorBrush(TextColor),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    TextAlignment = TextAlignment.Center,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = new RotateTransform(90) // ⭐ rotate clockwise
                };

                // Center horizontally
                //double textX = x + (barWidth / 2) - 15; // adjust for text width
                double textX = x + (barWidth / 2) - (valueText.FontSize / 2);
                Canvas.SetLeft(valueText, textX - 1);

                // If bar is tall enough, place text inside; otherwise place above
                if (barHeight > 22)
                {
                    Canvas.SetTop(valueText, height - barHeight + 4); // inside bar
                    //valueText.Foreground = new SolidColorBrush(TextColor);
                }
                else
                {
                    Canvas.SetTop(valueText, height - barHeight - 18); // above bar
                    //valueText.Foreground = new SolidColorBrush(TextColor);
                }

                PART_BarCanvas.Children.Add(valueText);


                x += barWidth + spacing;
            }

            // Bind labels
            //IEnumerable<string> amounts = points.Select(p => $"{p.TimeSpent.TotalHours:0.0}h");
            //PART_LabelPanel.ItemsSource = amounts;
        }

        #region [Not Used]
        public IEnumerable<ChartPoint> ItemsSource
        {
            get => (IEnumerable<ChartPoint>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
        
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource),
                typeof(IEnumerable<ChartPoint>),
                typeof(BarChart),
                new PropertyMetadata(null, OnItemsSourceChanged));

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BarChart chart)
                chart.Redraw();
        }

        void Redraw()
        {
            PART_BarCanvas.Children.Clear();

            if (ItemsSource == null)
                return;

            var points = ItemsSource.ToList();
            if (points.Count == 0)
                return;

            double maxValue = points.Max(p => p.Value);
            if (maxValue <= 0)
                maxValue = 1;

            double width = PART_BarCanvas.ActualWidth;
            double height = PART_BarCanvas.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                // Wait for layout
                Loaded += (_, __) => Redraw();
                return;
            }

            double barWidth = width / points.Count * 0.6;
            double spacing = width / points.Count * 0.4;

            double x = spacing / 2;

            foreach (var p in points)
            {
                double barHeight = (p.Value / maxValue) * height;

                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = barHeight,
                    RadiusX = 4,
                    RadiusY = 4,
                    Fill = new SolidColorBrush(Color.FromRgb(100, 160, 255)), // soft blue
                    Stroke = new SolidColorBrush(Color.FromRgb(70, 120, 200)),
                    StrokeThickness = 1,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, height - barHeight);

                PART_BarCanvas.Children.Add(rect);


                //
                // VALUE INSIDE BAR
                //
                var valueText = new TextBlock
                {
                    Text = $"{p.Value:0.0}",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center
                };

                // Center horizontally
                double textX = x + (barWidth / 2) - 15; // adjust for text width
                Canvas.SetLeft(valueText, textX);

                // If bar is tall enough, place text inside; otherwise place above
                if (barHeight > 22)
                {
                    Canvas.SetTop(valueText, height - barHeight + 4); // inside bar
                }
                else
                {
                    Canvas.SetTop(valueText, height - barHeight - 18); // above bar
                    valueText.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                }

                PART_BarCanvas.Children.Add(valueText);


                x += barWidth + spacing;
            }

            // Bind labels
            //IEnumerable<string> amounts = points.Select(p => $"{p.TimeSpent.TotalHours:0.0}h");
            //PART_LabelPanel.ItemsSource = amounts;
        }
        #endregion
    }
}

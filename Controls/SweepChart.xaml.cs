using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using TimeLogger.Models;

namespace TimeLogger.Controls
{
    public partial class SweepChart : UserControl
    {
        double _sweepX;
        bool _drawLabels = false;
        DateTime _lastFrame;
        Pen gridPen = new Pen(new SolidColorBrush(Color.FromArgb(130, 35, 35, 35)), 1);
        Pen glowPen = new Pen(new SolidColorBrush(Color.FromArgb(70, 0, 148, 255)), 6);
        Pen tracePen = new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 108, 255)), 2);
        Pen sweepPen1 = new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 108, 255)), 2);
        Pen sweepPen2 = new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 108, 255)), 1);
        SolidColorBrush bkgndBrush = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
        readonly List<(Point ScreenPoint, ChartPoint DataPoint)> _screenPoints = new List<(Point, ChartPoint)>();
        public event EventHandler<ChartPointClickedEventArgs>? ChartPointClicked;

        ChartPoint _hoveredPoint;
        Point _hoveredScreenPoint;

        public SweepChart()
        {
            InitializeComponent();

            Loaded += SweepChart_Loaded;
            Unloaded += SweepChart_Unloaded;
            MouseLeftButtonDown += SweepChart_MouseLeftButtonDown;

            MouseMove += HeartbeatChart_MouseMove;
            MouseLeave += HeartbeatChart_MouseLeave;

            this.ToolTip = new ToolTip();
        }

        void HeartbeatChart_MouseMove(object sender, MouseEventArgs e)
        {
            Point mouse = e.GetPosition(this);
            const double hitRadius = 10;
            _hoveredPoint = null;
            foreach (var item in _screenPoints)
            {
                double dx = item.ScreenPoint.X - mouse.X;
                double dy = item.ScreenPoint.Y - mouse.Y;

                if ((dx * dx) + (dy * dy) <= hitRadius * hitRadius)
                {
                    _hoveredPoint = item.DataPoint;
                    _hoveredScreenPoint = item.ScreenPoint;
                    this.ToolTip = new ToolTip
                    {
                        Content = $"{item.DataPoint.Title}\n{item.DataPoint.Time:G}\n{item.DataPoint.Value:N2} {item.DataPoint.Uom}", 
                    };
                    ToolTipService.SetIsEnabled(this, true);
                    InvalidateVisual();
                    return;
                }
            }
            this.ToolTip = null;
            InvalidateVisual();
        }

        void HeartbeatChart_MouseLeave(object sender, MouseEventArgs e)
        {
            _hoveredPoint = null;
            this.ToolTip = null;
            InvalidateVisual();
        }

        void DrawAxes(DrawingContext dc)
        {
            const double leftMargin = 60;
            const double bottomMargin = 30;

            Pen axisPen = new Pen(Brushes.Gray, 1);

            dc.DrawLine(
                axisPen,
                new Point(leftMargin, 0),
                new Point(leftMargin, ActualHeight - bottomMargin));

            dc.DrawLine(
                axisPen,
                new Point(leftMargin, ActualHeight - bottomMargin),
                new Point(ActualWidth, ActualHeight - bottomMargin));

            if (_drawLabels)
            {
                DrawYAxisLabels(dc, leftMargin);
                DrawXAxisLabels(dc, bottomMargin);
            }
        }

        void DrawYAxisLabels(DrawingContext dc, double leftMargin)
        {
            if (ItemsSource == null || ItemsSource.Count == 0)
                return;

            double min = ItemsSource.Min(x => x.Value);
            double max = ItemsSource.Max(x => x.Value);

            const int divisions = 5;

            for (int i = 0; i <= divisions; i++)
            {
                double ratio = (double)i / divisions;

                double value =
                    max - ((max - min) * ratio);

                double y =
                    ratio * (ActualHeight - 30);

                DrawText(
                    dc,
                    value.ToString("N0"),
                    4,
                    y - 8);
            }
        }

        void DrawXAxisLabels(DrawingContext dc, double bottomMargin)
        {
            DateTime weekStart = GetWeekStart(DateTime.Now);

            for (int day = 0; day < 7; day++)
            {
                DateTime currentDay =
                    weekStart.AddDays(day);

                double x =
                    (ActualWidth / 7.0) * day;

                DrawText(
                    dc,
                    currentDay.ToString("ddd"),
                    x + 5,
                    ActualHeight - 24);
            }
        }


        void DrawText(DrawingContext dc, string text, double x, double y)
        {
            FormattedText formattedText =
                new FormattedText(
                    text,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    12,
                    Brushes.LightGray,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(
                formattedText,
                new Point(x, y));
        }


        void DrawHoveredPoint(DrawingContext dc)
        {
            if (_hoveredPoint == null)
                return;

            dc.DrawEllipse(
                Brushes.DodgerBlue,
                new Pen(Brushes.White, 1),
                _hoveredScreenPoint,
                3,
                3);
        }




        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(ObservableCollection<ChartPoint>),
                typeof(SweepChart),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public ObservableCollection<ChartPoint> ItemsSource
        {
            get => (ObservableCollection<ChartPoint>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SweepPixelsPerSecondProperty =
            DependencyProperty.Register(
                nameof(SweepPixelsPerSecond),
                typeof(double),
                typeof(SweepChart),
                new PropertyMetadata(150.0));

        public double SweepPixelsPerSecond
        {
            get => (double)GetValue(SweepPixelsPerSecondProperty);
            set => SetValue(SweepPixelsPerSecondProperty, value);
        }

        public static readonly DependencyProperty DayAmountProperty =
            DependencyProperty.Register(
                nameof(DayAmount),
                typeof(double),
                typeof(SweepChart),
                new PropertyMetadata(7.0));

        public double DayAmount
        {
            get => (double)GetValue(DayAmountProperty);
            set => SetValue(DayAmountProperty, value);
        }

        public Color SweepPen1Color
        {
            get => (Color)GetValue(SweepPen1ColorProperty);
            set => SetValue(SweepPen1ColorProperty, value);
        }
        public static readonly DependencyProperty SweepPen1ColorProperty = DependencyProperty.Register(
            nameof(SweepPen1Color),
            typeof(Color),
            typeof(SweepChart),
            new PropertyMetadata(Color.FromArgb(110, 0, 108, 255)));

        public Color SweepPen2Color
        {
            get => (Color)GetValue(SweepPen2ColorProperty);
            set => SetValue(SweepPen2ColorProperty, value);
        }
        public static readonly DependencyProperty SweepPen2ColorProperty = DependencyProperty.Register(
            nameof(SweepPen2Color),
            typeof(Color),
            typeof(SweepChart),
            new PropertyMetadata(Color.FromArgb(110, 0, 80, 255)));

        public Color TracePenColor
        {
            get => (Color)GetValue(TracePenColorProperty);
            set => SetValue(TracePenColorProperty, value);
        }
        public static readonly DependencyProperty TracePenColorProperty = DependencyProperty.Register(
            nameof(TracePenColor),
            typeof(Color),
            typeof(SweepChart),
            new PropertyMetadata(Color.FromArgb(110, 0, 108, 255)));

        public Color GlowPenColor
        {
            get => (Color)GetValue(GlowPenColorProperty);
            set => SetValue(GlowPenColorProperty, value);
        }
        public static readonly DependencyProperty GlowPenColorProperty = DependencyProperty.Register(
            nameof(GlowPenColor),
            typeof(Color),
            typeof(SweepChart),
            new PropertyMetadata(Color.FromArgb(70, 0, 148, 255)));

        public Color GridPenColor
        {
            get => (Color)GetValue(GridPenColorProperty);
            set => SetValue(GridPenColorProperty, value);
        }
        public static readonly DependencyProperty GridPenColorProperty = DependencyProperty.Register(
            nameof(GridPenColor),
            typeof(Color),
            typeof(SweepChart),
            new PropertyMetadata(Color.FromArgb(70, 35, 35, 35)));

        public Color BackgroundColor
        {
            get => (Color)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }
        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(
            nameof(BackgroundColor),
            typeof(Color),
            typeof(SweepChart),
            new PropertyMetadata(Color.FromArgb(150, 0, 0, 0)));

        public static readonly DependencyProperty DrawBackgroundProperty = DependencyProperty.Register(
            nameof(DrawBackground),
            typeof(bool),
            typeof(SweepChart),
            new PropertyMetadata(false));
        public bool DrawBackground
        {
            get => (bool)GetValue(DrawBackgroundProperty);
            set => SetValue(DrawBackgroundProperty, value);
        }

        #endregion

        void SweepChart_Loaded(object sender, RoutedEventArgs e)
        {
            _lastFrame = DateTime.Now;
            #region [Build Pen Brushes]
            sweepPen1 = new Pen(new SolidColorBrush(SweepPen1Color), 2);
            sweepPen2 = new Pen(new SolidColorBrush(SweepPen2Color), 2);
            tracePen = new Pen(new SolidColorBrush(TracePenColor), 2);
            glowPen = new Pen(new SolidColorBrush(GlowPenColor), 6);
            gridPen = new Pen(new SolidColorBrush(GridPenColor), 1);
            bkgndBrush = new SolidColorBrush(BackgroundColor);
            #endregion
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void SweepChart_Unloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            double elapsedSeconds = (now - _lastFrame).TotalSeconds;

            _lastFrame = now;

            _sweepX += SweepPixelsPerSecond * elapsedSeconds;

            // Loop back to start (with small buffer to prevent hard-cut)
            if (_sweepX > ActualWidth + (ActualWidth * 0.06))
                _sweepX = 0;

            InvalidateVisual(); // Trigger redraw
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            if (DrawBackground)
                dc.DrawRectangle(bkgndBrush, null, new Rect(0, 0, width, height));

            if (_drawLabels)
                DrawAxes(dc);

            DrawGrid(dc);

            if (ItemsSource == null || ItemsSource.Count == 0)
                return;

            //DateTime weekStart = GetNDayWeekStart(DateTime.Now, DayAmount);
            DateTime weekStart = GetDayStart(DayAmount);
            DateTime weekEnd = weekStart.AddDays(DayAmount);

            var weekData = ItemsSource
                .Where(p => p.Time >= weekStart && p.Time <= weekEnd)
                .OrderBy(p => p.Time)
                .ToList();

            if (weekData.Count < 2)
                return;

            double minValue = weekData.Min(x => x.Value);
            double maxValue = weekData.Max(x => x.Value);

            if (Math.Abs(maxValue - minValue) < 0.0001)
            {
                maxValue += 1;
            }

            _screenPoints.Clear();

            foreach (ChartPoint point in weekData)
            {
                Point screenPoint = ConvertToScreenPoint(point, weekStart, minValue, maxValue);
                _screenPoints.Add((screenPoint, point));
            }

            DrawTrace(dc);
            DrawSweepLine(dc);

            if (_drawLabels)
                DrawHoveredPoint(dc);
        }

        void DrawGrid(DrawingContext dc)
        {
            for (int x = 0; x < ActualWidth; x += 50)
            {
                dc.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
            }
            for (int y = 0; y < ActualHeight; y += 50)
            {
                dc.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
            }
        }

        void DrawTrace(DrawingContext dc)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                bool started = false;
                foreach (var item in _screenPoints)
                {
                    if (item.ScreenPoint.X > _sweepX)
                        break;

                    if (!started)
                    {
                        ctx.BeginFigure(item.ScreenPoint, false, false);
                        started = true;
                    }
                    else
                    {
                        ctx.LineTo(item.ScreenPoint, true, true);
                    }
                }
            }
            geometry.Freeze();
            dc.DrawGeometry(null, glowPen, geometry);
            dc.DrawGeometry(null, tracePen, geometry);
        }

        void DrawSweepLine(DrawingContext dc)
        {
            dc.DrawLine(sweepPen1, new Point(_sweepX, 0), new Point(_sweepX, ActualHeight));
            dc.DrawLine(sweepPen2, new Point(_sweepX-2, 0), new Point(_sweepX-2, ActualHeight));
        }

        Point ConvertToScreenPoint(ChartPoint point, DateTime weekStart, double minValue, double maxValue)
        {
            double totalSeconds = TimeSpan.FromDays(DayAmount).TotalSeconds;
            double elapsedSeconds = (point.Time - weekStart).TotalSeconds;
            double x = (elapsedSeconds / totalSeconds) * ActualWidth;
            double y = ActualHeight - (((point.Value - minValue) / (maxValue - minValue)) * ActualHeight);
            return new Point(x, y);
        }

        /// <summary>
        /// For test only, use <see cref="Get7DayWeekStart(DateTime)"/>
        /// </summary>
        static DateTime GetWeekStart(DateTime date)
        {
            int diff = (1 + (date.DayOfWeek - DayOfWeek.Monday)) % 1;
            return date.Date.AddDays(-diff);
        }

        static DateTime Get7DayWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        static DateTime GetNDayWeekStart(DateTime date, double days)
        {
            int diff = (int)((days + (date.DayOfWeek - DayOfWeek.Monday)) % days);
            return date.Date.AddDays(-diff);
        }

        static DateTime GetDayStart(double days)
        {
            int diff = (int)days;
            return DateTime.Now.AddDays(-diff);
        }

        void SweepChart_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mouse = e.GetPosition(this);
            const double hitRadius = 8;
            foreach (var item in _screenPoints)
            {
                double dx = item.ScreenPoint.X - mouse.X;
                double dy = item.ScreenPoint.Y - mouse.Y;

                if ((dx * dx) + (dy * dy) <= hitRadius * hitRadius)
                {
                    ChartPointClicked?.Invoke(this, new ChartPointClickedEventArgs(item.DataPoint));
                    break;
                }
            }
        }
    }
}
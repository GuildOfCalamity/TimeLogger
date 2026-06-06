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
        bool _drawBackground = false;
        DateTime _lastFrame;
        readonly List<(Point ScreenPoint, ChartPoint DataPoint)> _screenPoints = new List<(Point, ChartPoint)>();
        public event EventHandler<ChartPointClickedEventArgs>? ChartPointClicked;

        public SweepChart()
        {
            InitializeComponent();

            Loaded += SweepChart_Loaded;
            Unloaded += SweepChart_Unloaded;
            MouseLeftButtonDown += SweepChart_MouseLeftButtonDown;
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

        #endregion

        void SweepChart_Loaded(object sender, RoutedEventArgs e)
        {
            _lastFrame = DateTime.Now;
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

            if (_sweepX > ActualWidth)
                _sweepX = 0; // Loop back to start

            InvalidateVisual(); // Trigger redraw
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double width = ActualWidth;
            double height = ActualHeight;

            if (width <= 0 || height <= 0)
                return;

            if (_drawBackground)
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)), null, new Rect(0, 0, width, height));

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
        }

        void DrawGrid(DrawingContext dc)
        {
            Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(130, 35, 35, 35)), 1);

            for (int x = 0; x < ActualWidth; x += 50)
            {
                dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
            }

            for (int y = 0; y < ActualHeight; y += 50)
            {
                dc.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
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

            Pen glowPen = new Pen(new SolidColorBrush(Color.FromArgb(50, 0, 148, 255)), 8);
            Pen tracePen = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 108, 255)), 2);
            dc.DrawGeometry(null, glowPen, geometry);
            dc.DrawGeometry(null, tracePen, geometry);
        }

        void DrawSweepLine(DrawingContext dc)
        {
            Pen sweepPen = new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 108, 255)), 2);
            dc.DrawLine(sweepPen, new Point(_sweepX, 0), new Point(_sweepX, ActualHeight));
            Pen sweepPen2 = new Pen(new SolidColorBrush(Color.FromArgb(110, 0, 108, 255)), 1);
            dc.DrawLine(sweepPen2, new Point(_sweepX-1, 0), new Point(_sweepX-1, ActualHeight));
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
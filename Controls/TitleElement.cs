using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimeLogger.Controls;

public enum TitleAlignment
{
    Left,
    Top
}

public class TitleElement
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.RegisterAttached(
        "Title", 
        typeof(string), 
        typeof(TitleElement), 
        new PropertyMetadata(default(string)));

    public static void SetTitle(DependencyObject element, string value) => element.SetValue(TitleProperty, value);

    public static string GetTitle(DependencyObject element) => (string)element.GetValue(TitleProperty);

    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached(
        "Background", 
        typeof(Brush), 
        typeof(TitleElement), 
        new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetBackground(DependencyObject element, Brush value) => element.SetValue(BackgroundProperty, value);

    public static Brush GetBackground(DependencyObject element) => (Brush)element.GetValue(BackgroundProperty);

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached(
        "Foreground", 
        typeof(Brush), 
        typeof(TitleElement), 
        new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetForeground(DependencyObject element, Brush value) => element.SetValue(ForegroundProperty, value);

    public static Brush GetForeground(DependencyObject element) => (Brush)element.GetValue(ForegroundProperty);

    public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.RegisterAttached(
        "BorderBrush", 
        typeof(Brush), 
        typeof(TitleElement), 
        new FrameworkPropertyMetadata(default(Brush), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetBorderBrush(DependencyObject element, Brush value) => element.SetValue(BorderBrushProperty, value);

    public static Brush GetBorderBrush(DependencyObject element) => (Brush)element.GetValue(BorderBrushProperty);

    public static readonly DependencyProperty TitleAlignmentProperty = DependencyProperty.RegisterAttached(
        "TitleAlignment", 
        typeof(TitleAlignment), 
        typeof(TitleElement), 
        new FrameworkPropertyMetadata(TitleAlignment.Top, FrameworkPropertyMetadataOptions.Inherits));

    public static void SetTitleAlignment(DependencyObject element, TitleAlignment value) => element.SetValue(TitleAlignmentProperty, value);

    public static TitleAlignment GetTitleAlignment(DependencyObject element)
        => (TitleAlignment)element.GetValue(TitleAlignmentProperty);

    public static readonly DependencyProperty TitleWidthProperty = DependencyProperty.RegisterAttached(
        "TitleWidth", 
        typeof(GridLength), 
        typeof(TitleElement), 
        new FrameworkPropertyMetadata(new GridLength(120.0), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetTitleWidth(DependencyObject element, GridLength value) => element.SetValue(TitleWidthProperty, value);

    public static GridLength GetTitleWidth(DependencyObject element) => (GridLength)element.GetValue(TitleWidthProperty);
}

public class InfoElement : TitleElement
{
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder", 
        typeof(string), 
        typeof(InfoElement), 
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetPlaceholder(DependencyObject element, string value) => element.SetValue(PlaceholderProperty, value);

    public static string GetPlaceholder(DependencyObject element) => (string)element.GetValue(PlaceholderProperty);

    public static readonly DependencyProperty NecessaryProperty = DependencyProperty.RegisterAttached(
        "Necessary", 
        typeof(bool), 
        typeof(InfoElement), 
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static void SetNecessary(DependencyObject element, bool value) => element.SetValue(NecessaryProperty, value);

    public static bool GetNecessary(DependencyObject element) => (bool)element.GetValue(NecessaryProperty);

    public static readonly DependencyProperty SymbolProperty = DependencyProperty.RegisterAttached(
        "Symbol", 
        typeof(string), 
        typeof(InfoElement), 
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.Inherits));

    public static void SetSymbol(DependencyObject element, string value) => element.SetValue(SymbolProperty, value);

    public static string GetSymbol(DependencyObject element) => (string)element.GetValue(SymbolProperty);

    public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.RegisterAttached(
        "ContentHeight", 
        typeof(double), 
        typeof(InfoElement), 
        new FrameworkPropertyMetadata(30.0, FrameworkPropertyMetadataOptions.Inherits));

    public static void SetContentHeight(DependencyObject element, double value) => element.SetValue(ContentHeightProperty, value);

    public static double GetContentHeight(DependencyObject element) => (double)element.GetValue(ContentHeightProperty);

    public static readonly DependencyProperty MinContentHeightProperty = DependencyProperty.RegisterAttached(
        "MinContentHeight", 
        typeof(double), 
        typeof(InfoElement), 
        new PropertyMetadata(30.0));

    public static void SetMinContentHeight(DependencyObject element, double value) => element.SetValue(MinContentHeightProperty, value);

    public static double GetMinContentHeight(DependencyObject element) => (double)element.GetValue(MinContentHeightProperty);

    public static readonly DependencyProperty MaxContentHeightProperty = DependencyProperty.RegisterAttached(
        "MaxContentHeight", 
        typeof(double), 
        typeof(InfoElement), 
        new PropertyMetadata(double.PositiveInfinity));

    public static void SetMaxContentHeight(DependencyObject element, double value) => element.SetValue(MaxContentHeightProperty, value);

    public static double GetMaxContentHeight(DependencyObject element) => (double)element.GetValue(MaxContentHeightProperty);
}

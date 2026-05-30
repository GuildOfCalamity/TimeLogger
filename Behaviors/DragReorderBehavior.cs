using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace TimeLogger.Behaviors;

public class DragReorderBehavior : Behavior<ListBox>
{
    private Point _dragStart;
    private bool _isDragging;

    protected override void OnAttached()
    {
        AssociatedObject.PreviewMouseLeftButtonDown += OnMouseDown;
        AssociatedObject.PreviewMouseMove += OnMouseMove;
        AssociatedObject.PreviewMouseLeftButtonUp += OnMouseUp;
        AssociatedObject.PreviewDrop += OnPreviewDrop;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseLeftButtonDown -= OnMouseDown;
        AssociatedObject.PreviewMouseMove -= OnMouseMove;
        AssociatedObject.PreviewMouseLeftButtonUp -= OnMouseUp;
        AssociatedObject.PreviewDrop -= OnPreviewDrop;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _isDragging = false;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        var pos = e.GetPosition(null);
        var diff = _dragStart - pos;

        if (!_isDragging &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            _isDragging = true;

            var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item == null)
                return;

            DragDrop.DoDragDrop(item, item.DataContext, DragDropEffects.Move);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }

    private void OnPreviewDrop(object sender, DragEventArgs e)
    {
        var listBox = AssociatedObject;
        var draggedItem = e.Data.GetData(typeof(object));
        var targetItem = GetItemUnderMouse(listBox, e.GetPosition(listBox));

        if (draggedItem == null || targetItem == null || draggedItem == targetItem)
            return;

        var items = (IList)listBox.ItemsSource;

        int oldIndex = items.IndexOf(draggedItem);
        int newIndex = items.IndexOf(targetItem);

        if (oldIndex != newIndex)
        {
            items.RemoveAt(oldIndex);
            items.Insert(newIndex, draggedItem);
        }
    }

    private object GetItemUnderMouse(ListBox listBox, Point position)
    {
        var element = listBox.InputHitTest(position) as DependencyObject;
        var container = FindAncestor<ListBoxItem>(element);
        return container?.DataContext;
    }

    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
                return match;

            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}

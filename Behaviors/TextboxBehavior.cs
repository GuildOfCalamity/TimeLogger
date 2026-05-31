using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace TimeLogger.Behaviors
{
    public class FocusOnVisibleBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            MakeFocus();
            AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.IsVisibleChanged -= OnIsVisibleChanged;
            base.OnDetaching();
        }

        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MakeFocus();
        }

        void MakeFocus()
        {
            if (AssociatedObject.Focusable)
                AssociatedObject.Focus();
        }
    }

    public class SelectAllOnFocusBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            MakeSelection();
            AssociatedObject.GotFocus += OnGotFocus;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= OnGotFocus;
            base.OnDetaching();
        }

        void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            MakeSelection();
        }

        void MakeSelection()
        {
            if (AssociatedObject.IsFocused)
            {
                Dispatcher.BeginInvoke(new Action(() => AssociatedObject.SelectAll()));
            }
        }
    }
}

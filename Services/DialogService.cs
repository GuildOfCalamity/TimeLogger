using System;
using System.Windows;

namespace TimeLogger.Services;

public interface IDialogService
{
    /// <summary>
    /// Window owner for the dialogs. 
    /// This is used to ensure that dialogs are on the main window and they are modal to it.
    /// </summary>
    MainWindow? Instance { get; }

    /// <summary>
    /// Same as <see cref="ShowInfo(string)"/> but with a more generic name. 
    /// This is the default method for showing messages to the user.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    bool? Show(string message);

    /// <summary>
    /// Same as <see cref="ShowInfo(string)"/> but with a more generic name. 
    /// This is the default method for showing messages to the user.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    bool? ShowBig(string message);

    /// <summary>
    /// Same as <see cref="Show(string)"/> but with a specific name. 
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    bool? ShowInfo(string message);

    /// <summary>
    /// Changes the icon to warning and shows a message.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    bool? ShowWarning(string message);

    /// <summary>
    /// Offers a message with OK and Cancel buttons and returns a boolean indicating the user's choice.
    /// Returns true if the user clicks OK, false if the user clicks Cancel.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    bool? ShowOKCancel(string message);

    /// <summary>
    /// Runs the specified action on the UI thread. If the current thread is the UI thread, 
    /// it executes the action immediately; otherwise, it dispatches the action to the UI thread.
    /// </summary>
    /// <param name="action">The action to run on the UI thread.</param>
    void RunOnUI(Action action);
}

public class DialogService : IDialogService
{
    public MainWindow? Instance { get; set; } = null;

    public DialogService(MainWindow? owner = null)
    {
        Instance = owner ?? Application.Current.MainWindow as MainWindow ?? new MainWindow();
    }

    public bool? Show(string message) => ShowInfo(message);

    public bool? ShowBig(string message)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            return WpfMessageBox.Show(message, false, false, owner: Instance);
        else
            return Application.Current.Dispatcher.Invoke(() => WpfMessageBox.Show(message, false, false, fontSize: 24, owner: Instance));
    }

    public bool? ShowInfo(string message)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            return WpfMessageBox.Show(message, false, false, owner: Instance);
        else
            return Application.Current.Dispatcher.Invoke(() => WpfMessageBox.Show(message, false, false, owner: Instance));
    }

    public bool? ShowWarning(string message)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            return WpfMessageBox.Show(message, false, true, owner: Instance);
        else
            return Application.Current.Dispatcher.Invoke(() => WpfMessageBox.Show(message, false, true, owner: Instance));
    }

    public bool? ShowOKCancel(string message)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            return WpfMessageBox.Show(message, true, false, owner: Instance);
        else
            return Application.Current.Dispatcher.Invoke(() => WpfMessageBox.Show(message, true, false, owner: Instance));
    }

    public void RunOnUI(Action action)
    {
        if (Application.Current.Dispatcher.CheckAccess())
            action();
        else
            Application.Current.Dispatcher.Invoke(action);
    }
}

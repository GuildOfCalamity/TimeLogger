using System;
using System.Windows;

namespace TimeLogger.Services;

public interface IDialogService
{
    /// <summary>
    /// Same as <see cref="ShowInfo(string)"/> but with a more generic name. 
    /// This is the default method for showing messages to the user.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    bool? Show(string message);

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
}

public class DialogService : IDialogService
{
    MainWindow? Instance { get; set; } = null;

    public DialogService(MainWindow? owner = null)
    {
        Instance = owner ?? Application.Current.MainWindow as MainWindow ?? new MainWindow();
    }

    public bool? Show(string message) => ShowInfo(message);

    public bool? ShowInfo(string message) => WpfMessageBox.Show(message, false, false, owner: Instance);

    public bool? ShowWarning(string message) => WpfMessageBox.Show(message, false, true, owner: Instance);

    public bool? ShowOKCancel(string message) => WpfMessageBox.Show(message, true, false, owner: Instance);
}

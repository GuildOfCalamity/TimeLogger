using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TimeLogger.Models;
using TimeLogger.Services;

namespace TimeLogger.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    #region [Properties]
    public event PropertyChangedEventHandler? PropertyChanged;
    void Notify([CallerMemberName] string? prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    public ObservableCollection<TaskEntry> Entries { get; } = new();
    public string DescriptionInput { get; set; }
    public string UrlInput { get; set; }
    public string TimeInput { get; set; }
    public ICommand AddEntryCommand { get; }
    #endregion

    #region [Constructor using MainWindow Instance]
    MainWindow? WindowInstance { get; set; } = null;
    public MainViewModel(MainWindow mainWindow)
    {
        WindowInstance = mainWindow;
        AddEntryCommand = new RelayCommand(AddEntry);

        // Load persisted data
        _ = LoadAsyncDescending();

        // Save whenever entries change
        Entries.CollectionChanged += async (_, __) =>
        {
            await DataStore.SaveAsync(Entries);
            Notify(nameof(TodayTotalDisplay));
            Notify(nameof(WeekTotalDisplay));
        };
    }
    #endregion

    #region [Constructor using IDialogService]
    readonly IDialogService _dialogService;
    public MainViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        AddEntryCommand = new RelayCommand(AddEntry);

        // Load persisted data
        _ = LoadAsyncDescending();

        // Save whenever entries change
        Entries.CollectionChanged += async (_, __) =>
        {
            await DataStore.SaveAsync(Entries);
            Notify(nameof(TodayTotalDisplay));
            Notify(nameof(WeekTotalDisplay));
        };
    }
    #endregion

    #region [Business Logic]
    async Task LoadAsyncAscending()
    {
        var loaded = await DataStore.LoadAsync();
        foreach (var entry in loaded.OrderBy(e => e.Date))
            Entries.Add(entry);

        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
    }

    async Task LoadAsyncDescending()
    {
        var loaded = await DataStore.LoadAsync();

        foreach (var entry in loaded.OrderByDescending(e => e.Date))
            Entries.Add(entry);

        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
    }

    void AddEntry()
    {
        if (string.IsNullOrWhiteSpace(TimeInput))
        {
            //WpfMessageBox.Show($"You must enter some data for the time.", false, true, owner: WindowInstance);

            // If in non-UI thread:
            //WindowInstance?.Dispatcher.Invoke(() => WpfMessageBox.Show($"ConfigManager error:{Environment.NewLine}", false, true, owner: WindowInstance));

            // If using IDialogService
            _dialogService?.ShowWarning($"You must enter some data for the time amount.");

            #region [Old-School (Not Recommended)]
            //Application.Current.Dispatcher.Invoke(() => { MessageBox.Show("Warning!"); });
            //await Application.Current.Dispatcher.InvokeAsync(() => { MessageBox.Show("Warning!"); });
            #endregion

            return;
        }

        var entry = new TaskEntry
        {
            Description = DescriptionInput,
            Url = !string.IsNullOrWhiteSpace(UrlInput) ? UrlInput : "https://azuredevops.com",
            TimeSpent = WorkTimeParser.Parse(TimeInput),
            Date = DateTime.Today
        };

        //Entries.Add(entry);

        // Insert newest first
        int index = 0;
        while (index < Entries.Count && Entries[index].Date >= entry.Date)
            index++;

        Entries.Insert(index, entry);


        DescriptionInput = "";
        UrlInput = "";
        TimeInput = "";

        Notify(nameof(DescriptionInput));
        Notify(nameof(UrlInput));
        Notify(nameof(TimeInput));
    }

    public string TodayTotalDisplay =>
        FormatTime(Entries.Where(e => e.Date == DateTime.Today)
                          .Aggregate(TimeSpan.Zero, (a, b) => a + b.TimeSpent));

    public string WeekTotalDisplay =>
        FormatTime(Entries.Where(e => IsSameBusinessWeek(e.Date))
                          .Aggregate(TimeSpan.Zero, (a, b) => a + b.TimeSpent));

    static bool IsSameBusinessWeek(DateTime date)
    {
        var today = DateTime.Today;
        int diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        if (diff < 0) diff += 7;

        var monday = today.AddDays(-diff);
        var friday = monday.AddDays(4);

        return date >= monday && date <= friday;
    }

    static string FormatTime(TimeSpan ts)
    {
        List<string> parts = new();

        if (ts.Days > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");

        return parts.Count == 0 ? "0m" : string.Join(" ", parts);
    }
    #endregion
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
    public bool UseBusinessWeek { get; set; }
    public string? DescriptionInput { get; set; }
    public string? UrlInput { get; set; }
    public string? TimeInput { get; set; }
    public string? DefaultUrl { get; set; }
    public ICommand AddEntryCommand { get; }
    public ICommand EditEntryCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand DoubleClickCommand { get; }
    TaskEntry? _selectedEntry;
    public TaskEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            _selectedEntry = value;
            Notify();

            if (value != null)
                PopulateInputsFromSelected(value);
        }
    }
    DateTime _newEntryDate = DateTime.Now;
    public DateTime NewEntryDate
    {
        get => _newEntryDate;
        set
        {
            _newEntryDate = value;
            Notify();
        }
    }
    public string TodayTotalDisplay
    {
        get
        {
            return Extensions.FormatTime(Entries.Where(e => e.Date == DateTime.Today)
                  .Aggregate(TimeSpan.Zero, (a, b) => a + b.TimeSpent));
        }
    }
    public string WeekTotalDisplay
    {
        get
        {
            if (UseBusinessWeek)
            {
                return Extensions.FormatTime(Entries.Where(e => Extensions.IsSameBusinessWeek(e.Date))
                     .Aggregate(TimeSpan.Zero, (a, b) => a + b.TimeSpent));
            }
            else
            {
                return Extensions.FormatTime(Entries.Where(e => Extensions.IsSameSevenDayWeek(e.Date))
                    .Aggregate(TimeSpan.Zero, (a, b) => a + b.TimeSpent));
            }
        }
    }
    #endregion

    #region [Constructor using IDialogService]
    readonly IDialogService _dialogService;
    public MainViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        AddEntryCommand = new RelayCommand(AddEntry);
        //EditEntryCommand = new RelayCommand(EditSelectedEntry);
        EditEntryCommand = new RelayCommand<TaskEntry>(EditEntry);
        DoubleClickCommand = new RelayCommand(EditSelectedEntry);
        DeleteEntryCommand = new RelayCommand<TaskEntry>(DeleteEntry);

        ConfigManager.OnError += (s, e) =>
        {
            _dialogService?.ShowWarning($"ConfigManager error:{Environment.NewLine}{e.Message}");
        };

        // Load app configs
        DefaultUrl = ConfigManager.Get("DefaultUrl", defaultValue: string.Empty);
        UseBusinessWeek = ConfigManager.Get("UseBusinessWeek", defaultValue: true);
        if (string.IsNullOrEmpty(DefaultUrl))
        {
            ConfigManager.Set(nameof(UseBusinessWeek), true, saveAfterUpdate: true);
            DefaultUrl = "https://azuredevops.com";
            ConfigManager.Set("DefaultUrl", "https://azuredevops.com", saveAfterUpdate: true); 
        }

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
    void PopulateInputsFromSelected(TaskEntry entry)
    {
        DescriptionInput = entry.Description;
        UrlInput = entry.Url;

        // Convert TimeSpan back to Jira-style format
        TimeInput = Extensions.FormatTime(entry.TimeSpent);

        Notify(nameof(DescriptionInput));
        Notify(nameof(UrlInput));
        Notify(nameof(TimeInput));
    }

    async Task LoadAsyncDescending()
    {
        var loaded = await DataStore.LoadAsync();

        foreach (var entry in loaded.OrderByDescending(e => e.Date))
            Entries.Add(entry);

        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
    }

    async Task LoadAsyncAscending()
    {
        var loaded = await DataStore.LoadAsync();
        foreach (var entry in loaded.OrderBy(e => e.Date))
            Entries.Add(entry);

        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
    }

    /// <summary>
    /// Inserts a <see cref="TaskEntry"/>.
    /// </summary>
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

        if (string.IsNullOrWhiteSpace(DescriptionInput))
        {
            _dialogService?.ShowWarning($"You must enter some data for the description");
            return;
        }

        var entry = new TaskEntry
        {
            Description = DescriptionInput,
            Url = !string.IsNullOrWhiteSpace(UrlInput) ? UrlInput : DefaultUrl,
            TimeSpent = WorkTimeParser.Parse(TimeInput),
            Date = NewEntryDate  // Date = DateTime.Now // Date = DateTime.Today
        }; 

        #region [Duplicate Check]
        bool isDuplicate = Entries.Any(e =>
            string.Equals(e.Description, entry.Description, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(e.Url, entry.Url, StringComparison.OrdinalIgnoreCase) &&
            (e.Date.Day == entry.Date.Day && e.Date.Hour == entry.Date.Hour && e.Date.Year == entry.Date.Year)
        );

        if (isDuplicate)
        {
            _dialogService?.ShowWarning("This task already exists for today.");
            return;
        }
        #endregion

        #region [Insert at end (oldest first)]
        //Entries.Add(entry);
        #endregion

        #region [Insert newest first]
        int index = 0;
        while (index < Entries.Count && Entries[index].Date >= entry.Date)
            index++;

        Entries.Insert(index, entry);
        #endregion

        #region [Clear out previous]
        DescriptionInput = "";
        UrlInput = "";
        TimeInput = "";
        #endregion

        Notify(nameof(DescriptionInput));
        Notify(nameof(UrlInput));
        Notify(nameof(TimeInput));
    }

    /// <summary>
    /// Passes the <see cref="TaskEntry"/> to the edit dialog.
    /// </summary>
    void EditEntry(TaskEntry entry)
    {
        if (entry == null)
        {
            _dialogService?.ShowWarning($"Empty TaskEntry, cannot continue.");
            return;
        }

        var dialog = new EditEntryWindow();
        var vm = new EditEntryViewModel(entry, result =>
        {
            dialog.DialogResult = result;
            dialog.Close();
        });

        dialog.DataContext = vm;
        dialog.Owner = _dialogService.Instance;

        bool? result = dialog.ShowDialog();

        // If user clicked Save, update the entry in the list.
        if (result == true)
        {
            int index = Entries.IndexOf(entry);
            Entries.RemoveAt(index);
            Entries.Insert(index, vm.EditedEntry);
            // Save will occur in Entries.CollectionChanged event handler.

            Notify(nameof(TodayTotalDisplay));
            Notify(nameof(WeekTotalDisplay));
        }
    }

    /// <summary>
    /// Select an entry in the UI then click Edit to modify it.
    /// </summary>
    void EditSelectedEntry()
    {
        if (SelectedEntry == null)
        {
            _dialogService?.Show($"Select an entry and then click Edit.");
            return;
        }

        var dialog = new EditEntryWindow();
        var vm = new EditEntryViewModel(SelectedEntry, result =>
        {
            dialog.DialogResult = result;
            dialog.Close();
        });

        dialog.DataContext = vm;
        dialog.Owner = _dialogService.Instance;

        bool? result = dialog.ShowDialog();

        // If user clicked Save, update the entry in the list.
        if (result == true)
        {
            int index = Entries.IndexOf(SelectedEntry);
            Entries.RemoveAt(index);
            Entries.Insert(index, vm.EditedEntry);
            // Save will occur in Entries.CollectionChanged event handler.

            Notify(nameof(TodayTotalDisplay));
            Notify(nameof(WeekTotalDisplay));
        }
    }

    void DeleteEntry(TaskEntry entry)
    {
        if (entry == null)
        {
            _dialogService?.ShowWarning($"Empty TaskEntry, cannot continue.");
            return;
        }

        Entries.Remove(entry);

        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
    }
    #endregion
}

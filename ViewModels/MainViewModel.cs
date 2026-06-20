using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using TimeLogger.Controls;
using TimeLogger.Models;
using TimeLogger.Services;

namespace TimeLogger.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    #region [Properties]
    static bool _loaded = false;
    public event PropertyChangedEventHandler? PropertyChanged;
    void Notify([CallerMemberName] string? prop = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    public ObservableCollection<TaskEntry> Entries { get; } = new();
    public double SweepSpeed { get; set; }
    public double FadeSeconds { get; set; }
    public bool UseBusinessWeek { get; set; }
    public bool BarChartPreferred { get; set; }
    public string? DescriptionInput { get; set; }
    public string? UrlInput { get; set; }
    public string? TimeInput { get; set; }
    public string? DefaultUrl { get; set; }

    public ICommand AddEntryCommand { get; }
    public ICommand ChartCommand { get; }
    public ICommand EditEntryCommand { get; }
    public ICommand DeleteEntryCommand { get; }
    public ICommand DoubleClickCommand { get; }
    public ICommand ChartPointSelectedCommand { get; }

    TaskEntry? _selectedEntry;
    public TaskEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            _selectedEntry = value;
            Notify();

            if (value != null)
            {
                PopulateInputsFromSelected(value);
                NewEntryDate = value.Date;
            }
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

    List<ChartSeries> _timeSeries = new List<ChartSeries>();
    public List<ChartSeries> TimeSeries
    {
        get => _timeSeries;
        set
        {
            _timeSeries = value;
            Notify();
        }
    }

    bool _chartVisible = false;
    public bool ChartVisible
    {
        get => _chartVisible;
        set
        {
            _chartVisible = value;
            Notify();
        }
    }
    #endregion

    #region [Constructor using IDialogService]
    readonly IDialogService _dialogService;
    public MainViewModel(IDialogService dialogService)
    {
        _dialogService = dialogService;
        _dialogService!.Instance!.Loaded += MainWindow_Loaded;
        
        #region [ICommands]
        AddEntryCommand = new RelayCommand(AddEntry);
        ChartCommand = new RelayCommand(ToggleChart);
        //EditEntryCommand = new RelayCommand(EditSelectedEntry);
        EditEntryCommand = new RelayCommand<TaskEntry>(EditEntry);
        DoubleClickCommand = new RelayCommand(EditSelectedEntry);
        DeleteEntryCommand = new RelayCommand<TaskEntry>(DeleteEntry);
        ChartPointSelectedCommand = new RelayCommand<ChartPoint>(OnChartPointSelected);
        #endregion

        ConfigManager.OnError += (s, e) =>
        {
            _dialogService?.ShowWarning($"ConfigManager error:{Environment.NewLine}{e.Message}");
        };

        #region [Load app configs]
        DefaultUrl = ConfigManager.Get("DefaultUrl", defaultValue: string.Empty);
        UseBusinessWeek = ConfigManager.Get("UseBusinessWeek", defaultValue: true);
        BarChartPreferred = ConfigManager.Get("BarChartPreferred", defaultValue: true);
        SweepSpeed = ConfigManager.Get("SweepSpeed", defaultValue: 100.0);
        FadeSeconds = ConfigManager.Get("FadeSeconds", defaultValue: 5.0);
        if (string.IsNullOrEmpty(DefaultUrl))
        {
            ConfigManager.Set(nameof(UseBusinessWeek), true, saveAfterUpdate: true);
            ConfigManager.Set(nameof(BarChartPreferred), true, saveAfterUpdate: true);
            DefaultUrl = "https://azuredevops.com";
            ConfigManager.Set("DefaultUrl", "https://azuredevops.com", saveAfterUpdate: true);
            ConfigManager.Set("SweepSpeed", 100.0, saveAfterUpdate: true);
            ConfigManager.Set("FadeSeconds", 5.0, saveAfterUpdate: true);
        }
        #endregion

        // Load persisted data
        _ = LoadAsyncDescending();

        // Save whenever entries change
        Entries.CollectionChanged += async (_, __) =>
        {
            if (_loaded)
                await DataStore.SaveAsync(Entries);
            Notify(nameof(TodayTotalDisplay));
            Notify(nameof(WeekTotalDisplay));
        };
    }

    /// <summary>
    /// We shouldn't have to do this if we bind the SweepChart properties directly to 
    /// the ViewModel, but since we're passing the MainWindow reference to the ViewModel 
    /// for other reasons, we can call this setup method on load to handle the point read.
    /// </summary>
    void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        Task.Run(async () =>
        {
            // Wait a moment to ensure the window visual tree is
            // loaded before trying to access the chart control.
            await Task.Delay(250);
            try
            {
                if (BarChartPreferred)
                {
                    window.barchart.Dispatcher.Invoke(() => 
                    { 
                        SetupBarChart(window.barchart); 
                        window.barchart.Visibility = Visibility.Visible;
                        window.sweep.Visibility = Visibility.Collapsed;
                    });
                }
                else
                {
                    window.sweep.Dispatcher.Invoke(() => 
                    { 
                        SetupSweepChart(window.sweep); 
                        window.sweep.Visibility = Visibility.Visible;
                        window.barchart.Visibility = Visibility.Collapsed;
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] While setting up chart: {ex.Message}");
            }
            finally
            {
                _loaded = true;
            }
        });
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

        #region [Update charts]
        TimeSeries = new List<ChartSeries> 
        { 
            new ChartSeries 
            { 
                Points = Entries.Select(e => new ChartPoint(e.Date, e.TimeSpent.TotalHours, "hours", e.Description)).ToList()
            } 
        };
        if (!ChartVisible)
        {
            ChartVisible = true;
        }
        else
        {
            // If chart is already visible, trigger redraw by toggling visibility
            ChartVisible = false;
            ChartVisible = true;
        }

        if (BarChartPreferred)
            SetupBarChart(_dialogService!.Instance!.barchart);
        else
            SetupSweepChart(_dialogService!.Instance!.sweep);
        #endregion

        #region [Clear out previous]
        DescriptionInput = "";
        UrlInput = "";
        TimeInput = "";
        #endregion

        Notify(nameof(DescriptionInput));
        Notify(nameof(UrlInput));
        Notify(nameof(TimeInput));
        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
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

        #region [Update charts]
        TimeSeries = new List<ChartSeries>
        {
            new ChartSeries
            {
                Points = Entries.Select(e => new ChartPoint(e.Date, e.TimeSpent.TotalHours, "hours", e.Description)).ToList()
            }
        };
        
        if (BarChartPreferred)
            SetupBarChart(_dialogService!.Instance!.barchart);
        else
            SetupSweepChart(_dialogService!.Instance!.sweep);
        #endregion

        Notify(nameof(TodayTotalDisplay));
        Notify(nameof(WeekTotalDisplay));
    }
    #endregion

    #region [Chart Triggers]
    /// <summary>
    /// <see cref="Controls.CartesianChart"/> event using <see cref="RelayCommand{T}"/>
    /// </summary>
    void OnChartPointSelected(ChartPoint cp)
    {
        try
        {
            var selection = Entries.Where(e => e.Date == cp.Time && e.Description == cp.Title).First();
            if (selection == null)
                return;

            SelectedEntry = selection;
        }
        catch { }
    }

    /// <summary>
    /// <see cref="Controls.CartesianChart"/> event for selection from MainWindow.xaml.cs
    /// </summary>
    public void SetPointSelection(ChartPoint c)
    {
        try
        {
            var selection = Entries.Where(e => e.Date == c.Time && e.Description == c.Title).First();
            if (selection == null)
                return;

            SelectedEntry = selection;
        }
        catch { }
    }

    /// <summary>
    /// <see cref="Controls.CartesianChart"/> event for test call from MainWindow.xaml.cs
    /// </summary>
    public void ShowChart(FrameworkElement chartElement, FrameworkElement listElement)
    {
        bool useRandomData = true;

        if (useRandomData)
        {
            List<ChartPoint> points = new List<ChartPoint>();
            for (int i = 1; i < 21; i++)
            {
                points.Add(new ChartPoint(DateTime.Now.Add(TimeSpan.FromHours(i)), Random.Shared.Next(3, 11), "hours", $"Test Entry #{i}"));
            }
            TimeSeries = new List<ChartSeries> { new ChartSeries { Points = points } };
        }

        if (TimeSeries == null || TimeSeries.Count == 0)
        {
            _dialogService.ShowWarning($"No chart data available, try adding entries first.");
            return;
        }
        if (chartElement == null || listElement == null)
        {
            _dialogService.ShowWarning($"No chart or list element found.");
            return;
        }

        if (chartElement is CartesianChart chart)
        {
            chartElement.Visibility = chartElement.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            listElement.Visibility = listElement.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
            if (chart.Visibility == Visibility.Visible)
                chart.Redraw();
        }
    }

    /// <summary>
    /// <see cref="Controls.CartesianChart"/> event for MVVM
    /// </summary>
    void ToggleChart()
    {
        bool useRandomData = false;

        if (useRandomData)
        {
            List<ChartPoint> points = new List<ChartPoint>();
            for (int i = 1; i < 21; i++)
            {
                points.Add(new ChartPoint(DateTime.Now.Add(TimeSpan.FromHours(i)), Random.Shared.Next(3, 11), "hours", $"Test Entry #{i}"));
            }
            TimeSeries = new List<ChartSeries> { new ChartSeries { Points = points } };
        }
        else
        {
            if (Entries.Count == 0)
            {
                _dialogService.ShowWarning($"No chart data available, try adding entries first.");
                return;
            }
            var points = Entries.Select(e => new ChartPoint(e.Date, e.TimeSpent.TotalHours, "hours", e.Description)).ToList();
            TimeSeries = new List<ChartSeries> { new ChartSeries { Points = points } };
        }
        ChartVisible = !ChartVisible;
    }

    public void SetupSweepChart(SweepChart sweep)
    {
        if (sweep == null)
            return;

        var points = Entries.Select(e => new ChartPoint(e.Date, e.TimeSpent.TotalHours, "hours", e.Description)).ToList();
        sweep.ItemsSource = new ObservableCollection<Models.ChartPoint>();
        foreach (var cp in points)
        {
            sweep.ItemsSource.Add(cp);
        }
    }

    public void SetupBarChart(BarChart barchart)
    {
        if (barchart == null)
            return;

        var grouped = Entries
            .GroupBy(t => t.Date.Date) // normalize to date only
            .Select(g => new TaskEntry
            {
                Date = g.Key,
                TimeSpent = TimeSpan.FromTicks(g.Sum(x => x.TimeSpent.Ticks)),
                Description = g.Count() == 1 ? "1 entry" : $"{g.Count()} entries",
                Url = DefaultUrl ?? string.Empty
            })
            .OrderBy(x => x.Date)
            .ToList();


        barchart.Entries = grouped;
    }
    #endregion
}

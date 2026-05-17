using System.Diagnostics;
using System.IO;
using System.Text.Json;
using TimeLogger.Models;

namespace TimeLogger.Services;

public static class DataStore
{
    static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimeLogger");
    static readonly string FilePath = Path.Combine(Folder, "entries.json");
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static async Task SaveAsync(IEnumerable<TaskEntry> entries)
    {
        if (!Directory.Exists(Folder))
            Directory.CreateDirectory(Folder);

        using FileStream fs = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(fs, entries, Options);
    }

    public static async Task<List<TaskEntry>> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new List<TaskEntry>(); // first run, no data

        Debug.WriteLine($"[INFO] Loading data from {FilePath}");

        using FileStream fs = File.OpenRead(FilePath);
        var result = await JsonSerializer.DeserializeAsync<List<TaskEntry>>(fs, Options);
        return result ?? new List<TaskEntry>();
    }
}

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

        try
        {
            using FileStream fs = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(fs, entries, Options);
        }
        catch (IOException ex) 
        {
            Debug.WriteLine($"[WARNING] Failed to save data to {FilePath}: {ex.Message}");
            await Task.Delay(100);
            try
            {
                using FileStream fs = File.Create(FilePath);
                await JsonSerializer.SerializeAsync(fs, entries, Options);
            }
            catch { }
        }
    }

    public static async Task<List<TaskEntry>> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new List<TaskEntry>(); // first run, no data

        Debug.WriteLine($"[INFO] Loading data from {FilePath}");

        try
        {
            using FileStream fs = File.OpenRead(FilePath);
            var result = await JsonSerializer.DeserializeAsync<List<TaskEntry>>(fs, Options);
            return result ?? new List<TaskEntry>();
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"[WARNING] Failed to load data from {FilePath}: {ex.Message}");
            await Task.Delay(100);
            try
            {
                using FileStream fs = File.OpenRead(FilePath);
                var result = await JsonSerializer.DeserializeAsync<List<TaskEntry>>(fs, Options);
                return result ?? new List<TaskEntry>();
            }
            catch 
            {
                throw; // total failure, bubble up the exception
            }
        }

    }
}

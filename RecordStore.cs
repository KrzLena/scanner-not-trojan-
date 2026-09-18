using System.Text.Json;

namespace MauiApp1;

public static class RecordStore
{
    private static string FilePath =>
        Path.Combine(FileSystem.AppDataDirectory, "records.json");

    public static async Task<List<PersonRecord>> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new List<PersonRecord>();

        try
        {
            string json = await File.ReadAllTextAsync(FilePath);

            return JsonSerializer.Deserialize<List<PersonRecord>>(json)
                   ?? new List<PersonRecord>();
        }
        catch
        {
            return new List<PersonRecord>();
        }
    }

    public static async Task SaveAsync(List<PersonRecord> records)
    {
        string json = JsonSerializer.Serialize(
            records,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await File.WriteAllTextAsync(FilePath, json);
    }
}
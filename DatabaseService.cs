using SQLite;
using System.Text;

namespace MauiApp1;

public static class DatabaseService
{
    private static SQLiteAsyncConnection? _database;
    private static bool _initialized = false;

    public static string DatabasePath =>
        Path.Combine(
            FileSystem.AppDataDirectory,
            "records.db3"
        );

    private static async Task Init()
    {
        if (_initialized && _database != null)
            return;

        _database =
            new SQLiteAsyncConnection(DatabasePath);

        await _database.CreateTableAsync<PersonRecord>();

        _initialized = true;

        var records = await _database
            .Table<PersonRecord>()
            .ToListAsync();

        foreach (var record in records)
        {
            bool changed = false;

            if (!record.CreatedAt.HasValue &&
                record.LegacyScannedAt.HasValue)
            {
                record.CreatedAt =
                    record.LegacyScannedAt;

                changed = true;
            }

            if (!record.LastScannedAt.HasValue &&
                record.LegacyScannedAt.HasValue)
            {
                record.LastScannedAt =
                    record.LegacyScannedAt;

                changed = true;
            }

            if (string.IsNullOrWhiteSpace(record.QrCode))
            {
                record.IsManual = true;
                record.QrCode = record.FullName;
                changed = true;
            }

            if (changed)
                await _database.UpdateAsync(record);
        }
    }

    public static async Task<List<PersonRecord>>
        GetRecordsAsync()
    {
        await Init();

        return await _database!
            .Table<PersonRecord>()
            .ToListAsync();
    }

    public static async Task<PersonRecord?>
        GetRecordAsync(int id)
    {
        await Init();

        return await _database!
            .Table<PersonRecord>()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public static async Task AddRecordAsync(
        PersonRecord record)
    {
        await Init();

        await _database!.InsertAsync(record);
    }

    public static async Task UpdateRecordAsync(
        PersonRecord record)
    {
        await Init();

        await _database!.UpdateAsync(record);
    }

    public static async Task DeleteRecordAsync(
        PersonRecord record)
    {
        await Init();

        await _database!.DeleteAsync(record);
    }

    public static async Task DeleteRecordsAsync(
        List<PersonRecord> records)
    {
        await Init();

        foreach (var record in records)
        {
            await _database!.DeleteAsync(record);
        }
    }

    public static async Task<PersonRecord?>
        FindDuplicateAsync(
            string qr,
            string firstName,
            string lastName)
    {
        var records = await GetRecordsAsync();

        return records.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(qr) &&
             x.QrCode.Equals(
                 qr,
                 StringComparison.OrdinalIgnoreCase))
            ||
            (
                x.FirstName.Equals(
                    firstName,
                    StringComparison.OrdinalIgnoreCase)
                &&
                x.LastName.Equals(
                    lastName,
                    StringComparison.OrdinalIgnoreCase)
            )
        );
    }


    public static async Task<string> ExportCsvAsync()
    {
        var records = await GetRecordsAsync();

        StringBuilder csv = new();

        csv.AppendLine(
            "Imie;Nazwisko;KodQR;Utworzono;OstatniSkan;DodanoRecznie"
        );

        foreach (var record in records)
        {
            csv.AppendLine(
                Csv(record.FirstName) + ";" +
                Csv(record.LastName) + ";" +
                Csv(record.QrCode) + ";" +
                Csv(record.CreatedAt?.ToString("O") ?? "") + ";" +
                Csv(record.LastScannedAt?.ToString("O") ?? "") + ";" +
                Csv(record.IsManual.ToString())
            );
        }

        string path = Path.Combine(
            FileSystem.CacheDirectory,
            "rekordy_" +
            DateTime.Now.ToString("yyyyMMdd_HHmm") +
            ".csv"
        );

        await File.WriteAllTextAsync(
            path,
            csv.ToString()
        );

        return path;
    }

    public static async Task<(int imported, int skipped)>
        ImportCsvAsync(string csv)
    {
        await Init();

        int imported = 0;
        int skipped = 0;

        var existing = await GetRecordsAsync();

        string[] lines = csv
            .Replace("\r", "")
            .Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var values = ParseCsvLine(lines[i]);

            if (values.Count < 6)
            {
                skipped++;
                continue;
            }

            string firstName = values[0];
            string lastName = values[1];
            string qr = values[2];

            bool duplicate = existing.Any(x =>
                !string.IsNullOrWhiteSpace(qr) &&
                x.QrCode.Equals(
                    qr,
                    StringComparison.OrdinalIgnoreCase)
            );

            if (duplicate)
            {
                skipped++;
                continue;
            }

            DateTime? created = null;
            DateTime? scanned = null;

            if (DateTime.TryParse(values[3], out var c))
                created = c;

            if (DateTime.TryParse(values[4], out var s))
                scanned = s;

            bool.TryParse(values[5], out bool manual);

            var record = new PersonRecord
            {
                FirstName = firstName,
                LastName = lastName,
                QrCode = string.IsNullOrWhiteSpace(qr)
                    ? firstName + " " + lastName
                    : qr,
                CreatedAt = created ?? DateTime.Now,
                LastScannedAt = scanned,
                IsManual = manual
            };

            await _database!.InsertAsync(record);

            existing.Add(record);
            imported++;
        }

        return (imported, skipped);
    }

    private static string Csv(string value)
    {
        return "\"" +
               value.Replace("\"", "\"\"") +
               "\"";
    }

    private static List<string> ParseCsvLine(
        string line)
    {
        List<string> values = new();

        StringBuilder value = new();

        bool quotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (quotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                {
                    quotes = !quotes;
                }
            }
            else if (c == ';' && !quotes)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(c);
            }
        }

        values.Add(value.ToString());

        return values;
    }

    public static async Task<string> BackupAsync()
    {
        await Init();

        string backupPath = Path.Combine(
            FileSystem.CacheDirectory,
            "records_backup_" +
            DateTime.Now.ToString("yyyyMMdd_HHmmss") +
            ".db3"
        );

        await _database!.CloseAsync();

        _database = null;
        _initialized = false;

        File.Copy(
            DatabasePath,
            backupPath,
            true
        );

        await Init();

        return backupPath;
    }
}

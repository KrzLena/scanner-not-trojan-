using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace MauiApp1;

public partial class RecordsPage : ContentPage
{
    private List<PersonRecord> _records = new();

    public RecordsPage()
    {
        InitializeComponent();

        SortPicker.SelectedIndex = 0;
        DatePickerFilter.SelectedIndex = 0;

        DeleteButton.IsEnabled = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadRecords();
    }

    private async Task LoadRecords()
    {
        _records =
            await DatabaseService.GetRecordsAsync();

        foreach (var record in _records)
        {
            record.IsSelected = false;
        }

        DeleteButton.IsEnabled = false;

        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<PersonRecord> result =
            _records;

        string search =
            SearchBox.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(search))
        {
            result = result.Where(x =>
                x.FullName.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase)
                ||
                x.QrCode.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase)
            );
        }

        if (DatePickerFilter.SelectedIndex == 1)
        {
            result = result.Where(x =>
                x.ActivityDate.HasValue &&
                x.ActivityDate.Value.Date ==
                DateTime.Today
            );
        }
        else if (DatePickerFilter.SelectedIndex == 2)
        {
            DateTime from =
                DateTime.Now.AddDays(-7);

            result = result.Where(x =>
                x.ActivityDate.HasValue &&
                x.ActivityDate.Value >= from
            );
        }

        switch (SortPicker.SelectedIndex)
        {
            case 1:
                result = result
                    .OrderBy(x => x.ActivityDate);
                break;

            case 2:
                result = result
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName);
                break;

            case 3:
                result = result
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName);
                break;

            default:
                result = result
                    .OrderByDescending(
                        x => x.ActivityDate);
                break;
        }

        var list = result.ToList();

        RecordsView.ItemsSource = null;
        RecordsView.ItemsSource = list;

        CountLabel.Text =
            $"Wyświetlono: {list.Count} / {_records.Count}";

        UpdateDeleteButton();
    }

    private void SearchChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void FilterChanged(
        object sender,
        EventArgs e)
    {
        ApplyFilters();
    }

    private void SelectionChanged(
        object sender,
        CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox &&
            checkBox.BindingContext is PersonRecord record)
        {
            record.IsSelected = e.Value;
        }

        UpdateDeleteButton();
    }

    private void UpdateDeleteButton()
    {
        DeleteButton.IsEnabled =
            _records.Any(x => x.IsSelected);
    }

    private async void ScanClicked(
        object sender,
        EventArgs e)
    {
        var page =
            new ScanPage();

        await Navigation.PushAsync(page);

        Navigation.RemovePage(this);
    }

    private async void AddClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new EditPage(
                new PersonRecord(),
                true)
        );
    }

    private async void RecordTapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is Border border &&
            border.BindingContext
            is PersonRecord record)
        {
            await Navigation.PushAsync(
                new DetailsPage(record.Id)
            );
        }
    }

    private async void EditSwipeInvoked(
        object sender,
        EventArgs e)
    {
        if (sender is SwipeItem item &&
            item.CommandParameter
            is PersonRecord record)
        {
            await Navigation.PushAsync(
                new EditPage(record, false)
            );
        }
    }

    private async void DeleteSwipeInvoked(
        object sender,
        EventArgs e)
    {
        if (sender is not SwipeItem item ||
            item.CommandParameter
            is not PersonRecord record)
        {
            return;
        }

        bool answer =
            await DisplayAlert(
                "Usuń",
                $"Usunąć {record.FullName}?",
                "Tak",
                "Nie"
            );

        if (!answer)
            return;

        await DatabaseService
            .DeleteRecordAsync(record);

        await LoadRecords();
    }

    private async void DeleteClicked(
        object sender,
        EventArgs e)
    {
        var selected =
            _records
                .Where(x => x.IsSelected)
                .ToList();

        if (selected.Count == 0)
            return;

        bool answer =
            await DisplayAlert(
                "Usuń",
                $"Usunąć rekordy: {selected.Count}?",
                "Tak",
                "Nie"
            );

        if (!answer)
            return;

        await DatabaseService
            .DeleteRecordsAsync(selected);

        await LoadRecords();
    }

    private async void MoreClicked(
        object sender,
        EventArgs e)
    {
        string action =
            await DisplayActionSheet(
                "Więcej",
                "Anuluj",
                null,
                "Zaznacz wszystko",
                "Odznacz wszystko",
                "Statystyki",
                "Eksport CSV",
                "Import CSV",
                "Kopia bazy"
            );

        if (action == "Zaznacz wszystko")
        {
            foreach (var record in _records)
            {
                record.IsSelected = true;
            }

            ApplyFilters();
        }
        else if (action == "Odznacz wszystko")
        {
            foreach (var record in _records)
            {
                record.IsSelected = false;
            }

            ApplyFilters();
        }
        else if (action == "Statystyki")
        {
            await Navigation.PushAsync(
                new StatsPage()
            );
        }
        else if (action == "Eksport CSV")
        {
            await ExportCsv();
        }
        else if (action == "Import CSV")
        {
            await ImportCsv();
        }
        else if (action == "Kopia bazy")
        {
            await BackupDatabase();
        }
    }

    private async Task ExportCsv()
    {
        string path =
            await DatabaseService
                .ExportCsvAsync();

        await Share.Default.RequestAsync(
            new ShareFileRequest
            {
                Title = "Eksport rekordów",
                File = new ShareFile(path)
            }
        );
    }

    private async Task ImportCsv()
    {
        var file =
            await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle =
                        "Wybierz plik CSV"
                }
            );

        if (file == null)
            return;

        using var stream =
            await file.OpenReadAsync();

        using StreamReader reader =
            new(stream);

        string csv =
            await reader.ReadToEndAsync();

        var result =
            await DatabaseService
                .ImportCsvAsync(csv);

        await DisplayAlert(
            "Import",
            $"Dodano: {result.imported}\n" +
            $"Pominięto: {result.skipped}",
            "OK"
        );

        await LoadRecords();
    }

    private async Task BackupDatabase()
    {
        string path =
            await DatabaseService.BackupAsync();

        await Share.Default.RequestAsync(
            new ShareFileRequest
            {
                Title = "Kopia bazy danych",
                File = new ShareFile(path)
            }
        );
    }
}
